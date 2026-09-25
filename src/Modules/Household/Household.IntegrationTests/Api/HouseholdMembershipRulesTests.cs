namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

/// <summary>
/// #226 — the membership invariants enforced end-to-end through the HTTP surface:
/// last-owner rule, one active household per person, and leaving.
/// </summary>
public sealed class HouseholdMembershipRulesTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly HouseholdApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public HouseholdMembershipRulesTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> SignedInAsync(string name)
    {
        var client = _factory.CreateClientFor(
            $"auth|{Guid.NewGuid():N}", email: $"{Guid.NewGuid():N}@x.com", name: name);
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<Guid> PersonIdAsync(HttpClient client)
        => (await client.GetFromJsonAsync<Body>("/api/persons/me"))!.Id;

    private static async Task<Household> CreateHouseholdAsync(HttpClient client, string name)
    {
        (await client.PostAsJsonAsync("/api/households", new { name })).EnsureSuccessStatusCode();
        return (await client.GetFromJsonAsync<Household>("/api/households/me"))!;
    }

    private async Task<(HttpClient Owner, Household Household, HttpClient Adult, Guid AdultId)> HouseholdWithAdultAsync()
    {
        var owner = await SignedInAsync("Owner");
        var household = await CreateHouseholdAsync(owner, "The House");

        var adult = await SignedInAsync("Adult");
        var adultId = await PersonIdAsync(adult);
        var add = await owner.PostAsJsonAsync($"/api/households/{household.Id}/members",
            new { personId = adultId, role = "Adult", nickname = (string?)null });
        add.EnsureSuccessStatusCode();
        var invitationId = (await add.Content.ReadFromJsonAsync<AddResult>())!.InvitationId;

        (await adult.PostAsync($"/api/households/invitations/{invitationId}/accept", null))
            .EnsureSuccessStatusCode();

        return (owner, household, adult, adultId);
    }

    /// <summary>Demoting the only owner returns 422.</summary>
    [Fact]
    public async Task DemotingTheOnlyOwner_Returns422()
    {
        var (owner, household, _, _) = await HouseholdWithAdultAsync();
        var ownerId = await PersonIdAsync(owner);

        var demote = await owner.PutAsJsonAsync($"/api/households/{household.Id}/members/{ownerId}/role", new { role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);

        demote.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Removing the only owner returns 422.</summary>
    [Fact]
    public async Task RemovingTheOnlyOwner_Returns422()
    {
        var (owner, household, _, _) = await HouseholdWithAdultAsync();
        var ownerId = await PersonIdAsync(owner);

        var remove = await owner.DeleteAsync($"/api/households/{household.Id}/members/{ownerId}", TestContext.Current.CancellationToken);

        remove.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Cannot leave: <c>TheOnlyOwner</c> returns 422.</summary>
    [Fact]
    public async Task TheOnlyOwner_CannotLeave_Returns422()
    {
        var owner = await SignedInAsync("Solo Owner");
        var household = await CreateHouseholdAsync(owner, "Solo");

        var leave = await owner.PostAsync($"/api/households/{household.Id}/leave", null, TestContext.Current.CancellationToken);

        leave.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Adding a person who already has a household returns 422.</summary>
    [Fact]
    public async Task AddingAPersonWhoAlreadyHasAHousehold_Returns422()
    {
        var owner = await SignedInAsync("Owner");
        var household = await CreateHouseholdAsync(owner, "First House");

        var other = await SignedInAsync("Other");
        var otherId = await PersonIdAsync(other);
        await CreateHouseholdAsync(other, "Other House");

        var add = await owner.PostAsJsonAsync($"/api/households/{household.Id}/members", new { personId = otherId, role = "Adult", nickname = (string?)null }, cancellationToken: TestContext.Current.CancellationToken);

        add.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Can leave: <c>AnAdultMember</c> then has no household.</summary>
    [Fact]
    public async Task AnAdultMember_CanLeave_AndThenHasNoHousehold()
    {
        var (owner, household, adult, adultId) = await HouseholdWithAdultAsync();

        var leave = await adult.PostAsync($"/api/households/{household.Id}/leave", null, TestContext.Current.CancellationToken);
        leave.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await adult.GetAsync("/api/households/me", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var members = await owner.GetFromJsonAsync<List<Member>>($"/api/households/{household.Id}/members", cancellationToken: TestContext.Current.CancellationToken);
        members!.ShouldHaveSingleItem().PersonId.ShouldNotBe(adultId);
    }

    private sealed record Body(Guid Id);

    private sealed record AddResult(Guid InvitationId);

    private sealed record Household(Guid Id, string Name, string MyRole, List<Member> Members);

    private sealed record Member(Guid PersonId, string DisplayName, string Role, bool IsManaged);
}
