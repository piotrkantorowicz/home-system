namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

/// <summary>Integration tests for <c>HouseholdInvitation</c> against a real PostgreSQL container.</summary>
public sealed class HouseholdInvitationTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly HouseholdApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public HouseholdInvitationTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, Guid HouseholdId)> OwnerWithHouseholdAsync()
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: $"{Guid.NewGuid():N}@x.com", name: "Owner");
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        await client.PostAsJsonAsync("/api/households", new { name = "H" });
        var id = (await client.GetFromJsonAsync<Mine>("/api/households/me"))!.Id;
        return (client, id);
    }

    /// <summary>Unknown email: <c>Invite</c> creates a pending invitation that a mere sign-in does not resolve.</summary>
    [Fact]
    public async Task Invite_UnknownEmail_CreatesPending_AndSignInAloneDoesNotJoin()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var inviteeEmail = $"invitee-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email = inviteeEmail, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        invite.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await invite.Content.ReadFromJsonAsync<InviteResult>(cancellationToken: TestContext.Current.CancellationToken);
        result!.InvitationId.ShouldNotBe(Guid.Empty);

        var pending = await owner.GetFromJsonAsync<List<Invitation>>($"/api/households/{householdId}/invitations", cancellationToken: TestContext.Current.CancellationToken);
        pending!.ShouldHaveSingleItem().Email.ShouldBe(inviteeEmail);

        // The invitee signs in for the first time — this alone must not join the household.
        var invitee = _factory.CreateClientFor(
            $"auth|{Guid.NewGuid():N}", email: inviteeEmail.ToUpperInvariant(), name: "Invitee");
        (await invitee.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        (await invitee.GetAsync("/api/households/me", TestContext.Current.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // The invitation shows up as theirs and they explicitly accept it.
        var mine = await invitee.GetFromJsonAsync<List<MyInvitation>>("/api/households/invitations/mine", cancellationToken: TestContext.Current.CancellationToken);
        mine!.ShouldHaveSingleItem().HouseholdId.ShouldBe(householdId);

        var accept = await invitee.PostAsync($"/api/households/invitations/{result.InvitationId}/accept", null, TestContext.Current.CancellationToken);
        accept.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var inviteeHousehold = await invitee.GetFromJsonAsync<Mine>("/api/households/me", cancellationToken: TestContext.Current.CancellationToken);
        inviteeHousehold!.Id.ShouldBe(householdId);
        inviteeHousehold.MyRole.ShouldBe("Adult");

        var afterAccept = await owner.GetFromJsonAsync<List<Invitation>>($"/api/households/{householdId}/invitations", cancellationToken: TestContext.Current.CancellationToken);
        afterAccept!.ShouldBeEmpty();
    }

    /// <summary>Existing person not in a household: <c>Invite</c> creates a pending invitation, not immediate membership.</summary>
    [Fact]
    public async Task Invite_ExistingPersonNotInAHousehold_CreatesPendingInvitation()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();

        var otherEmail = $"other-{Guid.NewGuid():N}@example.com";
        var other = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: otherEmail, name: "Other");
        (await other.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var invite = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email = otherEmail, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        var result = await invite.Content.ReadFromJsonAsync<InviteResult>(cancellationToken: TestContext.Current.CancellationToken);
        result!.InvitationId.ShouldNotBe(Guid.Empty);

        (await other.GetAsync("/api/households/me", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var accept = await other.PostAsync($"/api/households/invitations/{result.InvitationId}/accept", null, TestContext.Current.CancellationToken);
        accept.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await other.GetFromJsonAsync<Mine>("/api/households/me", cancellationToken: TestContext.Current.CancellationToken))!.Id.ShouldBe(householdId);
    }

    /// <summary>Declining an invitation resolves it without granting membership.</summary>
    [Fact]
    public async Task Decline_MarksItResolved_WithoutJoining()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var inviteeEmail = $"declines-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email = inviteeEmail, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        var invitationId = (await invite.Content.ReadFromJsonAsync<InviteResult>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;

        var invitee = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: inviteeEmail, name: "Declines");
        (await invitee.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var decline = await invitee.PostAsync($"/api/households/invitations/{invitationId}/decline", null, TestContext.Current.CancellationToken);
        decline.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await invitee.GetAsync("/api/households/me", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var pending = await owner.GetFromJsonAsync<List<Invitation>>($"/api/households/{householdId}/invitations", cancellationToken: TestContext.Current.CancellationToken);
        pending!.ShouldBeEmpty();
    }

    /// <summary>A revoked invitation can no longer be accepted.</summary>
    [Fact]
    public async Task RevokedInvitation_CannotBeAccepted()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var email = $"revoked-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        var invitationId = (await invite.Content.ReadFromJsonAsync<InviteResult>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;

        var revoke = await owner.DeleteAsync($"/api/households/{householdId}/invitations/{invitationId}", TestContext.Current.CancellationToken);
        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var invitee = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: email, name: "Nope");
        (await invitee.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var accept = await invitee.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken);
        accept.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        (await invitee.GetAsync("/api/households/me", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Accepting an invitation addressed to someone else returns 403.</summary>
    [Fact]
    public async Task Accept_ByPersonTheInvitationIsNotAddressedTo_Returns403()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var email = $"target-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        var invitationId = (await invite.Content.ReadFromJsonAsync<InviteResult>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;

        var stranger = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: $"{Guid.NewGuid():N}@x.com", name: "Stranger");
        (await stranger.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var accept = await stranger.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken);
        accept.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// A different account that merely shares the targeted person's email cannot accept in their
    /// place. Regression: <c>PersonEmail</c> is not unique, so email must never bypass an
    /// authoritative <c>TargetPersonId</c> on a person-targeted invitation.
    /// </summary>
    [Fact]
    public async Task Accept_ByADifferentAccountSharingTheTargetsEmail_Returns403()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var sharedEmail = $"shared-{Guid.NewGuid():N}@example.com";

        var target = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: sharedEmail, name: "Target");
        (await target.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var targetId = (await target.GetFromJsonAsync<PersonBody>("/api/persons/me", cancellationToken: TestContext.Current.CancellationToken))!.Id;

        var impersonator = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: sharedEmail, name: "Impersonator");
        (await impersonator.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members", new { personId = targetId, role = "Adult", nickname = (string?)null }, cancellationToken: TestContext.Current.CancellationToken);
        var invitationId = (await add.Content.ReadFromJsonAsync<AddResult>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;

        var impersonatorAccept = await impersonator.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken);
        impersonatorAccept.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var targetAccept = await target.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken);
        targetAccept.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await target.GetFromJsonAsync<Mine>("/api/households/me", cancellationToken: TestContext.Current.CancellationToken))!.Id.ShouldBe(householdId);
    }

    /// <summary>
    /// An invitation past its 30-day lifetime is excluded from both the invitee's and the owner's
    /// lists and cannot be accepted, even though its status is never persisted as <c>Expired</c>
    /// (accepting inside a rolled-back ambient transaction cannot also flip and save that flag).
    /// </summary>
    [Fact]
    public async Task ExpiredInvitation_IsExcludedFromListsAndCannotBeAccepted()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var email = $"expired-{Guid.NewGuid():N}@example.com";

        var invite = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        var invitationId = (await invite.Content.ReadFromJsonAsync<InviteResult>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;

        _factory.Clock.Advance(TimeSpan.FromDays(31));

        var invitee = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: email, name: "TooLate");
        (await invitee.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var mine = await invitee.GetFromJsonAsync<List<MyInvitation>>("/api/households/invitations/mine", cancellationToken: TestContext.Current.CancellationToken);
        mine!.ShouldBeEmpty();

        var pending = await owner.GetFromJsonAsync<List<Invitation>>($"/api/households/{householdId}/invitations", cancellationToken: TestContext.Current.CancellationToken);
        pending!.ShouldBeEmpty();

        var accept = await invitee.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken);
        accept.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// An over-long nickname is trimmed and capped at 100 characters instead of failing at the
    /// database. Regression: the immediate-add path normalised it via
    /// <c>HouseholdMember.NormaliseNickname</c>; the invitation-issuing path that replaced it
    /// originally did not, and a 101-character nickname returned 500.
    /// </summary>
    [Fact]
    public async Task AddExistingPersonAsMember_WithAnOverLongNickname_TrimsItInsteadOfFailing()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();

        var target = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", email: $"{Guid.NewGuid():N}@x.com", name: "Target");
        (await target.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var targetId = (await target.GetFromJsonAsync<PersonBody>("/api/persons/me", cancellationToken: TestContext.Current.CancellationToken))!.Id;

        var overLong = new string('a', 150);
        var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members", new { personId = targetId, role = "Adult", nickname = overLong }, cancellationToken: TestContext.Current.CancellationToken);
        add.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invitationId = (await add.Content.ReadFromJsonAsync<AddResult>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;

        (await target.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        var members = await owner.GetFromJsonAsync<List<Member>>($"/api/households/{householdId}/members", cancellationToken: TestContext.Current.CancellationToken);
        members!.Single(m => m.PersonId == targetId).Nickname.ShouldBe(new string('a', 100));
    }

    /// <summary>Duplicate pending email: <c>Invite</c> returns 422.</summary>
    [Fact]
    public async Task Invite_DuplicatePendingEmail_Returns422()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var email = $"dup-{Guid.NewGuid():N}@example.com";

        await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);
        var second = await owner.PostAsJsonAsync($"/api/households/{householdId}/invitations", new { email, role = "Adult" }, cancellationToken: TestContext.Current.CancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record Mine(Guid Id, string Name, string MyRole);
    private sealed record InviteResult(Guid InvitationId);
    private sealed record AddResult(Guid InvitationId);
    private sealed record Invitation(Guid Id, string Email, string Role, string Status);
    private sealed record MyInvitation(Guid Id, Guid HouseholdId, string HouseholdName, string Role);
    private sealed record PersonBody(Guid Id);
    private sealed record Member(Guid PersonId, string? Nickname);
}
