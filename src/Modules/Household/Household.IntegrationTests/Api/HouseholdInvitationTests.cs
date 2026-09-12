namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

public sealed class HouseholdInvitationTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly HouseholdApiFactory _factory;

    public HouseholdInvitationTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    private async Task<(HttpClient Client, Guid HouseholdId)> OwnerWithHouseholdAsync()
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: $"{Guid.NewGuid():N}@x.com", name: "Owner");
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        await client.PostAsJsonAsync("/api/households", new { name = "H" });
        var id = (await client.GetFromJsonAsync<Mine>("/api/households/me"))!.Id;
        return (client, id);
    }

    [Fact]
    public async Task Invite_UnknownEmail_CreatesPending_ThenResolvesWhenThatUserLogsIn()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var inviteeEmail = $"invitee-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync(
            $"/api/households/{householdId}/invitations",
            new { email = inviteeEmail, role = "Adult" });
        invite.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await invite.Content.ReadFromJsonAsync<InviteResult>();
        result!.AddedImmediately.ShouldBeFalse();
        result.InvitationId.ShouldNotBeNull();

        var pending = await owner.GetFromJsonAsync<List<Invitation>>(
            $"/api/households/{householdId}/invitations");
        pending!.ShouldHaveSingleItem().Email.ShouldBe(inviteeEmail);

        // the invitee logs in for the first time
        var invitee = _factory.CreateClientFor(
            $"auth|{Guid.NewGuid():N}", email: inviteeEmail.ToUpperInvariant(), name: "Invitee");
        (await invitee.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();

        var inviteeHousehold = await invitee.GetFromJsonAsync<Mine>("/api/households/me");
        inviteeHousehold!.Id.ShouldBe(householdId);
        inviteeHousehold.MyRole.ShouldBe("Adult");

        var afterResolve = await owner.GetFromJsonAsync<List<Invitation>>(
            $"/api/households/{householdId}/invitations");
        afterResolve!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Invite_ExistingPersonNotInAHousehold_AddsThemImmediately()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();

        var otherEmail = $"other-{Guid.NewGuid():N}@example.com";
        var other = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: otherEmail, name: "Other");
        (await other.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();

        var invite = await owner.PostAsJsonAsync(
            $"/api/households/{householdId}/invitations",
            new { email = otherEmail, role = "Adult" });

        var result = await invite.Content.ReadFromJsonAsync<InviteResult>();
        result!.AddedImmediately.ShouldBeTrue();

        (await other.GetFromJsonAsync<Mine>("/api/households/me"))!.Id.ShouldBe(householdId);
    }

    [Fact]
    public async Task RevokedInvitation_DoesNotResolveOnLogin()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var email = $"revoked-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync(
            $"/api/households/{householdId}/invitations", new { email, role = "Adult" });
        var invitationId = (await invite.Content.ReadFromJsonAsync<InviteResult>())!.InvitationId;

        var revoke = await owner.DeleteAsync($"/api/households/{householdId}/invitations/{invitationId}");
        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var invitee = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: email, name: "Nope");
        (await invitee.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();

        (await invitee.GetAsync("/api/households/me")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invite_DuplicatePendingEmail_Returns422()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var email = $"dup-{Guid.NewGuid():N}@example.com";

        await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email, role = "Adult" });
        var second = await owner.PostAsJsonAsync(
            $"/api/households/{householdId}/invitations", new { email, role = "Adult" });

        second.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record Mine(Guid Id, string Name, string MyRole);
    private sealed record InviteResult(bool AddedImmediately, Guid? PersonId, Guid? InvitationId);
    private sealed record Invitation(Guid Id, string Email, string Role, string Status);
}
