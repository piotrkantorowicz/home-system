namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

/// <summary>
/// #220 — a managed member converted to an account keeps the same <c>PersonId</c> (and so
/// all personal data) when they first sign in.
/// </summary>
public sealed class ConvertManagedMemberToAccountTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly HouseholdApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public ConvertManagedMemberToAccountTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> OwnerWithHouseholdAsync()
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: $"{Guid.NewGuid():N}@x.com", name: "Owner");
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/households", new { name = "Convert House" })).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary><c>ConvertedManagedMember</c> keeps its person id and when it first signs in.</summary>
    [Fact]
    public async Task ConvertedManagedMember_KeepsItsPersonId_WhenItFirstSignsIn()
    {
        var owner = await OwnerWithHouseholdAsync();
        var household = (await owner.GetFromJsonAsync<MyHouseholdBody>("/api/households/me", cancellationToken: TestContext.Current.CancellationToken))!;

        var kidEmail = $"{Guid.NewGuid():N}@x.com";
        var add = await owner.PostAsJsonAsync($"/api/households/{household.Id}/managed-members", new { displayName = "Kiddo", email = (string?)null, role = "Child", nickname = (string?)null }, cancellationToken: TestContext.Current.CancellationToken);
        add.StatusCode.ShouldBe(HttpStatusCode.Created);
        var managedPersonId = Guid.Parse(add.Headers.Location!.ToString().Split("/")[^1]);

        var convert = await owner.PostAsJsonAsync($"/api/households/{household.Id}/members/{managedPersonId}/convert-to-account", new { email = kidEmail }, cancellationToken: TestContext.Current.CancellationToken);
        convert.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The kid signs in for the first time.
        var kid = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: kidEmail, name: "Kiddo Grown");
        var sync = await kid.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken);
        sync.EnsureSuccessStatusCode();
        var linkedPersonId = (await sync.Content.ReadFromJsonAsync<SyncBody>(cancellationToken: TestContext.Current.CancellationToken))!.PersonId;

        linkedPersonId.ShouldBe(managedPersonId);

        var me = await kid.GetFromJsonAsync<PersonMe>("/api/persons/me", cancellationToken: TestContext.Current.CancellationToken);
        me!.IsManaged.ShouldBeFalse();

        var members = await owner.GetFromJsonAsync<List<MemberBody>>($"/api/households/{household.Id}/members", cancellationToken: TestContext.Current.CancellationToken);
        var kiddo = members!.Single(m => m.PersonId == managedPersonId);
        kiddo.Role.ShouldBe("Child");
        kiddo.IsManaged.ShouldBeFalse();
    }

    /// <summary>By a non owner: <c>ConvertToAccount</c> is forbidden.</summary>
    [Fact]
    public async Task ConvertToAccount_ByANonOwner_IsForbidden()
    {
        var owner = await OwnerWithHouseholdAsync();
        var household = (await owner.GetFromJsonAsync<MyHouseholdBody>("/api/households/me", cancellationToken: TestContext.Current.CancellationToken))!;

        var add = await owner.PostAsJsonAsync($"/api/households/{household.Id}/managed-members", new { displayName = "Kiddo", email = (string?)null, role = "Child", nickname = (string?)null }, cancellationToken: TestContext.Current.CancellationToken);
        var managedPersonId = Guid.Parse(add.Headers.Location!.ToString().Split("/")[^1]);

        var stranger = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: $"{Guid.NewGuid():N}@x.com", name: "Nosy");
        (await stranger.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var convert = await stranger.PostAsJsonAsync($"/api/households/{household.Id}/members/{managedPersonId}/convert-to-account", new { email = $"{Guid.NewGuid():N}@x.com" }, cancellationToken: TestContext.Current.CancellationToken);

        convert.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed record MyHouseholdBody(Guid Id, string Name, string MyRole, List<MemberBody> Members);

    private sealed record MemberBody(Guid PersonId, string DisplayName, string Role, bool IsManaged);

    private sealed record SyncBody(Guid PersonId);

    private sealed record PersonMe(Guid Id, string DisplayName, string? Email, string? AvatarUrl, bool IsManaged);
}
