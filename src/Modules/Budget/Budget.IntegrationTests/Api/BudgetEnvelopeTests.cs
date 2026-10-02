namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#447 — envelope lifecycle and the visibility matrix, over both the collection and direct-ID routes.</summary>
public sealed class BudgetEnvelopeTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetEnvelopeTests(BudgetDatabaseFixture fixture) => _factory = new BudgetApiFactory(fixture);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpClient> SignedInAsync(string name)
    {
        var client = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", name);
        (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        return client;
    }

    private sealed record Household(HttpClient Owner, Guid Id, Guid OwnerPersonId);

    private async Task<Household> NewHouseholdAsync(bool initialiseBudget = true)
    {
        var owner = await SignedInAsync("Owner");
        (await owner.PostAsJsonAsync("/api/households", new { name = "The House" }, Ct)).EnsureSuccessStatusCode();
        var id = (await owner.GetFromJsonAsync<IdBody>("/api/households/me", Ct))!.Id;
        var ownerPersonId = (await owner.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
        if (initialiseBudget)
            (await owner.PostAsJsonAsync("/api/budget", new { }, Ct)).EnsureSuccessStatusCode();
        return new Household(owner, id, ownerPersonId);
    }

    private async Task<(HttpClient Client, Guid PersonId)> MemberAsync(Household h, string role)
    {
        var member = await SignedInAsync(role);
        var personId = (await member.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
        var add = await h.Owner.PostAsJsonAsync($"/api/households/{h.Id}/members", new { personId, role, nickname = (string?)null }, Ct);
        add.EnsureSuccessStatusCode();
        var invitationId = (await add.Content.ReadFromJsonAsync<InvitationBody>(Ct))!.InvitationId;
        (await member.PostAsync($"/api/households/invitations/{invitationId}/accept", null, Ct)).EnsureSuccessStatusCode();
        return (member, personId);
    }

    private static async Task<Guid> ManagedChildAsync(Household h)
    {
        var created = await h.Owner.PostAsJsonAsync($"/api/households/{h.Id}/managed-members",
            new { displayName = "Kid", email = (string?)null, role = "Child", nickname = (string?)null }, Ct);
        created.EnsureSuccessStatusCode();
        return Guid.Parse(created.Headers.Location!.ToString().Split('/').Last());
    }

    private static async Task<AccountBody> CreateAsync(HttpClient client, string name, string visibility, Guid? owner = null)
    {
        var response = await client.PostAsJsonAsync("/api/budget/accounts", new { name, visibility, ownerPersonId = owner }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AccountBody>(Ct))!;
    }

    private static async Task<PageBody> ListAsync(HttpClient client, string query = "")
        => (await client.GetFromJsonAsync<PageBody>($"/api/budget/accounts{query}", Ct))!;

    /// <summary>The default envelope is listed; new shared envelopes appear with revision 1.</summary>
    [Fact]
    public async Task Create_SharedEnvelope_IsListedWithRevisionOne()
    {
        var h = await NewHouseholdAsync();

        var created = await CreateAsync(h.Owner, "Holidays", "Household");
        var page = await ListAsync(h.Owner);

        created.Revision.ShouldBe(1);
        created.OwnerPersonId.ShouldBeNull();
        page.Items.Select(a => a.Name).ShouldBe(["Everyday", "Holidays"]);
        page.TotalCount.ShouldBe(2);
    }

    /// <summary>Before the budget exists, creating an envelope is a 404.</summary>
    [Fact]
    public async Task Create_WithoutBudget_Returns404()
    {
        var h = await NewHouseholdAsync(initialiseBudget: false);

        var response = await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "X", visibility = "Household" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Bad input is a 400.</summary>
    [Theory]
    [InlineData("", "Household")]
    [InlineData("X", "Secret")]
    [InlineData("X", "1")]
    public async Task Create_InvalidInput_Returns400(string name, string visibility)
    {
        var h = await NewHouseholdAsync();

        var response = await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name, visibility }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>A private adult envelope is invisible to other adults: list, count and direct ID.</summary>
    [Fact]
    public async Task PrivateAdultEnvelope_IsHiddenFromOtherAdults()
    {
        var h = await NewHouseholdAsync();
        var (adult, _) = await MemberAsync(h, "Adult");
        var mine = await CreateAsync(adult, "Adult private", "Personal");

        var ownerPage = await ListAsync(h.Owner);
        var adultPage = await ListAsync(adult);

        ownerPage.TotalCount.ShouldBe(1);
        ownerPage.Items.ShouldNotContain(a => a.Id == mine.Id);
        adultPage.TotalCount.ShouldBe(2);
        (await h.Owner.GetAsync($"/api/budget/accounts/{mine.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await adult.GetAsync($"/api/budget/accounts/{mine.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Even the owner cannot mutate it: indistinguishable from "does not exist".
        var rename = await h.Owner.PutAsJsonAsync($"/api/budget/accounts/{mine.Id}", new { name = "Mine now", expectedRevision = 1 }, Ct);
        rename.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{mine.Id}/archive", new { expectedRevision = 1 }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>An adult cannot create a personal envelope for another independent adult.</summary>
    [Fact]
    public async Task Create_PersonalForAnotherAdult_Returns403()
    {
        var h = await NewHouseholdAsync();
        var (_, adultPersonId) = await MemberAsync(h, "Adult");

        var response = await h.Owner.PostAsJsonAsync("/api/budget/accounts",
            new { name = "Sneaky", visibility = "Personal", ownerPersonId = adultPersonId }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A Child manages their own personal envelope, sees no shared ones, and cannot create shared ones.</summary>
    [Fact]
    public async Task Child_ManagesOwnPersonalEnvelopeOnly()
    {
        var h = await NewHouseholdAsync();
        var (child, childPersonId) = await MemberAsync(h, "Child");
        var shared = (await ListAsync(h.Owner)).Items.Single();

        var own = await CreateAsync(child, "Pocket money", "Personal");
        var renamed = await child.PutAsJsonAsync($"/api/budget/accounts/{own.Id}", new { name = "Savings", expectedRevision = 1 }, Ct);
        var childPage = await ListAsync(child);

        renamed.StatusCode.ShouldBe(HttpStatusCode.OK);
        own.OwnerPersonId.ShouldBe(childPersonId);
        childPage.Items.Select(a => a.Name).ShouldBe(["Savings"]);
        childPage.TotalCount.ShouldBe(1);
        (await child.GetAsync($"/api/budget/accounts/{shared.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await child.PostAsJsonAsync("/api/budget/accounts", new { name = "Shared", visibility = "Household" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await child.PostAsJsonAsync("/api/budget/accounts",
            new { name = "Other", visibility = "Personal", ownerPersonId = h.OwnerPersonId }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        // The child's private envelope is hidden from the household owner.
        (await ListAsync(h.Owner)).Items.ShouldNotContain(a => a.Id == own.Id);
    }

    /// <summary>A Guest has no access on any route.</summary>
    [Fact]
    public async Task Guest_HasNoAccess()
    {
        var h = await NewHouseholdAsync();
        var (guest, _) = await MemberAsync(h, "Guest");
        var shared = (await ListAsync(h.Owner)).Items.Single();

        (await guest.GetAsync("/api/budget/accounts", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await guest.GetAsync($"/api/budget/accounts/{shared.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await guest.PostAsJsonAsync("/api/budget/accounts", new { name = "X", visibility = "Personal" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Adults manage a managed member's envelope; another adult does see it too (both adults manage the dependant).</summary>
    [Fact]
    public async Task Adults_ManageManagedMembersEnvelope()
    {
        var h = await NewHouseholdAsync();
        var kid = await ManagedChildAsync(h);

        var created = await CreateAsync(h.Owner, "Kid allowance", "Personal", kid);
        var renamed = await h.Owner.PutAsJsonAsync($"/api/budget/accounts/{created.Id}", new { name = "Allowance", expectedRevision = 1 }, Ct);

        created.OwnerPersonId.ShouldBe(kid);
        renamed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ListAsync(h.Owner)).Items.ShouldContain(a => a.Id == created.Id);
    }

    /// <summary>Another household never sees or touches this household's envelopes.</summary>
    [Fact]
    public async Task OtherHousehold_CannotSeeOrChangeEnvelopes()
    {
        var a = await NewHouseholdAsync();
        var b = await NewHouseholdAsync();
        var shared = (await ListAsync(a.Owner)).Items.Single();

        (await ListAsync(b.Owner)).Items.ShouldNotContain(x => x.Id == shared.Id);
        (await b.Owner.GetAsync($"/api/budget/accounts/{shared.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await b.Owner.PutAsJsonAsync($"/api/budget/accounts/{shared.Id}", new { name = "Mine", expectedRevision = 1 }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Archive hides from the default list but keeps the envelope; restore undoes it; revisions advance.</summary>
    [Fact]
    public async Task ArchiveAndRestore_KeepTheEnvelope()
    {
        var h = await NewHouseholdAsync();
        var account = await CreateAsync(h.Owner, "Old trip", "Household");

        var archived = await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{account.Id}/archive", new { expectedRevision = 1 }, Ct);
        archived.StatusCode.ShouldBe(HttpStatusCode.OK);
        var archivedBody = (await archived.Content.ReadFromJsonAsync<AccountBody>(Ct))!;

        archivedBody.IsArchived.ShouldBeTrue();
        archivedBody.Revision.ShouldBe(2);
        (await ListAsync(h.Owner)).Items.ShouldNotContain(x => x.Id == account.Id);
        (await ListAsync(h.Owner, "?includeArchived=true")).Items.ShouldContain(x => x.Id == account.Id);
        (await h.Owner.GetAsync($"/api/budget/accounts/{account.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var restored = await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{account.Id}/restore", new { expectedRevision = 2 }, Ct);
        var restoredBody = (await restored.Content.ReadFromJsonAsync<AccountBody>(Ct))!;
        restoredBody.IsArchived.ShouldBeFalse();
        restoredBody.Revision.ShouldBe(3);
    }

    /// <summary>A stale revision is a 409 and changes nothing.</summary>
    [Fact]
    public async Task StaleRevision_Returns409()
    {
        var h = await NewHouseholdAsync();
        var account = await CreateAsync(h.Owner, "Trip", "Household");
        (await h.Owner.PutAsJsonAsync($"/api/budget/accounts/{account.Id}", new { name = "Trip 2", expectedRevision = 1 }, Ct))
            .EnsureSuccessStatusCode();

        var stale = await h.Owner.PutAsJsonAsync($"/api/budget/accounts/{account.Id}", new { name = "Trip 3", expectedRevision = 1 }, Ct);
        var staleArchive = await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{account.Id}/archive", new { expectedRevision = 1 }, Ct);

        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        staleArchive.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await h.Owner.GetFromJsonAsync<AccountBody>($"/api/budget/accounts/{account.Id}", Ct))!.Name.ShouldBe("Trip 2");
    }

    /// <summary>Two simultaneous renames with the same revision: one wins, the rest get 409.</summary>
    [Fact]
    public async Task ConcurrentRenames_OnlyOneSucceeds()
    {
        var h = await NewHouseholdAsync();
        var account = await CreateAsync(h.Owner, "Race", "Household");

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(i =>
            h.Owner.PutAsJsonAsync($"/api/budget/accounts/{account.Id}", new { name = $"R{i}", expectedRevision = 1 }, Ct)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(5);
    }

    /// <summary>Paging caps the page size and keeps the total across pages.</summary>
    [Fact]
    public async Task List_Paginates()
    {
        var h = await NewHouseholdAsync();
        await CreateAsync(h.Owner, "B", "Household");
        await CreateAsync(h.Owner, "C", "Household");

        var first = await ListAsync(h.Owner, "?page=1&pageSize=2");
        var second = await ListAsync(h.Owner, "?page=2&pageSize=2");
        var huge = await ListAsync(h.Owner, "?pageSize=5000");

        first.Items.Select(a => a.Name).ShouldBe(["B", "C"]);
        second.Items.Select(a => a.Name).ShouldBe(["Everyday"]);
        first.TotalCount.ShouldBe(3);
        huge.PageSize.ShouldBe(100);
    }

    private sealed record IdBody(Guid Id);
    private sealed record InvitationBody(Guid InvitationId);
    private sealed record AccountBody(Guid Id, string Name, string Visibility, Guid? OwnerPersonId, bool IsArchived, int Revision);
    private sealed record PageBody(List<AccountBody> Items, int TotalCount, int Page, int PageSize);
}
