namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#448 — recording expenses: money, stored shares, privacy, filters and retry safety, over HTTP to PostgreSQL.</summary>
public sealed class BudgetExpenseTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetExpenseTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object Body(
        Guid accountId, string amount = "100.00", string date = "2026-10-01", string category = "Groceries",
        string? funding = null, Guid? paidBy = null, Guid[]? participants = null, Guid? requestId = null)
        => new
        {
            clientRequestId = requestId ?? Guid.NewGuid(),
            accountId,
            amount,
            occurredOn = date,
            category,
            fundingSource = funding,
            paidByPersonId = paidBy,
            participantIds = participants,
        };

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, object body)
        => client.PostAsJsonAsync("/api/budget/expenses", body, Ct);

    private static async Task<ResultBody> CreatedAsync(HttpClient client, object body)
    {
        var response = await PostAsync(client, body);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
    }

    private static async Task<ExpenseBody> GetAsync(HttpClient client, Guid id)
        => (await client.GetFromJsonAsync<ExpenseBody>($"/api/budget/expenses/{id}", Ct))!;

    private static async Task<PageBody> ListAsync(HttpClient client, string query = "")
        => (await client.GetFromJsonAsync<PageBody>($"/api/budget/expenses{query}", Ct))!;

    private static async Task<Guid> PersonalEnvelopeAsync(HttpClient client, Guid? owner = null)
    {
        var response = await client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal", ownerPersonId = owner }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
    }

    /// <summary>100.00 split among three adults is stored exactly, recorder and payer differ, revision is 1.</summary>
    [Fact]
    public async Task SharedExpense_StoresExactSharesAndSeparatesPayerFromRecorder()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var cy = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        Guid[] everyone = [h.OwnerPersonId, bea.PersonId, cy.PersonId];

        var created = await CreatedAsync(h.Owner, Body(account, funding: "Individual", paidBy: bea.PersonId, participants: everyone));
        var expense = await GetAsync(bea.Client, created.ExpenseId);

        created.Revision.ShouldBe(1);
        expense.AddedByPersonId.ShouldBe(h.OwnerPersonId);
        expense.PaidByPersonId.ShouldBe(bea.PersonId);
        expense.Amount.ShouldBe("100.00");
        expense.Shares.Select(s => s.Amount).Order().ShouldBe(["33.33", "33.33", "33.34"]);
        expense.Shares.Sum(s => decimal.Parse(s.Amount, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(100m);
        expense.Shares.OrderBy(s => s.PersonId).First().Amount.ShouldBe("33.34");
    }

    /// <summary>Household funds give no personal credit and no shares.</summary>
    [Fact]
    public async Task HouseholdFunds_HaveNoPayerOrShares()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        var created = await CreatedAsync(h.Owner, Body(account, funding: "HouseholdFunds"));
        var expense = await GetAsync(h.Owner, created.ExpenseId);

        expense.PaidByPersonId.ShouldBeNull();
        expense.Shares.ShouldBeEmpty();
        (await PostAsync(h.Owner, Body(account, funding: "HouseholdFunds", paidBy: h.OwnerPersonId))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(h.Owner, Body(account, funding: "HouseholdFunds", participants: [h.OwnerPersonId]))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Bad amounts, categories, funding, dates and custom/spoofed fields are rejected.</summary>
    [Theory]
    [InlineData("0", "Groceries")]
    [InlineData("1.005", "Groceries")]
    [InlineData("1e2", "Groceries")]
    [InlineData("-5", "Groceries")]
    [InlineData("10000000000.00", "Groceries")]
    [InlineData("5", "Gambling")]
    [InlineData("5", "1")]
    public async Task InvalidAmountOrCategory_Returns400(string amount, string category)
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        (await PostAsync(h.Owner, Body(account, amount, category: category, funding: "HouseholdFunds"))).StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>Client-supplied shares, a spoofed recorder, or an empty request ID are rejected; the recorder is always the caller.</summary>
    [Fact]
    public async Task SpoofedActorOrCustomShares_Rejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var common = new { clientRequestId = Guid.NewGuid(), accountId = account, amount = "10.00", occurredOn = "2026-10-01", category = "Other", fundingSource = "HouseholdFunds" };

        var spoofed = await h.Owner.PostAsJsonAsync("/api/budget/expenses", new { common.clientRequestId, common.accountId, common.amount, common.occurredOn, common.category, common.fundingSource, addedByPersonId = bea.PersonId }, Ct);
        var shares = await h.Owner.PostAsJsonAsync("/api/budget/expenses", new { common.clientRequestId, common.accountId, common.amount, common.occurredOn, common.category, fundingSource = "Individual", paidByPersonId = h.OwnerPersonId, shares = new[] { new { personId = h.OwnerPersonId, amount = "10.00" } } }, Ct);
        var noId = await PostAsync(h.Owner, Body(account, funding: "HouseholdFunds", requestId: Guid.Empty));
        var ok = await CreatedAsync(h.Owner, Body(account, funding: "HouseholdFunds"));

        spoofed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        shares.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        noId.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await GetAsync(h.Owner, ok.ExpenseId)).AddedByPersonId.ShouldBe(h.OwnerPersonId);
    }

    /// <summary>Participant and payer rules: current adults only, no duplicates, at least one participant.</summary>
    [Fact]
    public async Task SharedExpense_RejectsBadParticipantsAndPayers()
    {
        var h = await _scenario.NewHouseholdAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var outsider = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        (await PostAsync(h.Owner, Body(account, paidBy: h.OwnerPersonId, participants: []))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(h.Owner, Body(account, paidBy: h.OwnerPersonId, participants: [h.OwnerPersonId, h.OwnerPersonId]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await PostAsync(h.Owner, Body(account, paidBy: h.OwnerPersonId, participants: [child.PersonId]))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(h.Owner, Body(account, paidBy: outsider.OwnerPersonId, participants: [h.OwnerPersonId]))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(h.Owner, Body(account, paidBy: child.PersonId, participants: [h.OwnerPersonId]))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(h.Owner, Body(account, participants: [h.OwnerPersonId]))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Archived and invisible envelopes cannot receive entries; unknown envelopes are 404.</summary>
    [Fact]
    public async Task Envelope_MustBeActiveAndVisible()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var other = await _scenario.NewHouseholdAsync();
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var beaPrivate = await PersonalEnvelopeAsync(bea.Client);
        var otherShared = await BudgetScenario.DefaultEnvelopeAsync(other.Owner);

        (await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{shared}/archive", new { expectedRevision = 1 }, Ct)).EnsureSuccessStatusCode();

        (await PostAsync(h.Owner, Body(shared, funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(h.Owner, Body(beaPrivate, funding: "Individual"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(h.Owner, Body(otherShared, funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(h.Owner, Body(Guid.NewGuid(), funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Personal expenses are paid by the owner, not shared, and invisible to other adults — in lists, counts and by ID.</summary>
    [Fact]
    public async Task PersonalExpenses_ArePrivate()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var beaPersonal = await PersonalEnvelopeAsync(bea.Client);

        var secret = await CreatedAsync(bea.Client, Body(beaPersonal, "42.00", category: "Leisure"));
        await CreatedAsync(h.Owner, Body(shared, "42.00", category: "Leisure", funding: "HouseholdFunds"));
        var detail = await GetAsync(bea.Client, secret.ExpenseId);

        detail.PaidByPersonId.ShouldBe(bea.PersonId);
        detail.Shares.ShouldBeEmpty();
        (await ListAsync(bea.Client)).TotalCount.ShouldBe(2);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(1);
        (await ListAsync(h.Owner, "?amount=42.00&category=Leisure")).TotalCount.ShouldBe(1);
        (await h.Owner.GetAsync($"/api/budget/expenses/{secret.ExpenseId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(bea.Client, Body(beaPersonal, funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(bea.Client, Body(beaPersonal, participants: [bea.PersonId]))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(bea.Client, Body(beaPersonal, paidBy: h.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>A Child records only in their own envelope; a Guest has no access; an adult records for a managed member.</summary>
    [Fact]
    public async Task Roles_ChildOwnOnly_GuestDenied_AdultForManagedMember()
    {
        var h = await _scenario.NewHouseholdAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var guest = await _scenario.MemberAsync(h, "Guest");
        var kid = await BudgetScenario.ManagedChildAsync(h);
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var childEnvelope = await PersonalEnvelopeAsync(child.Client);
        var kidEnvelope = await PersonalEnvelopeAsync(h.Owner, kid);

        (await PostAsync(child.Client, Body(childEnvelope))).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await PostAsync(child.Client, Body(shared, funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(guest.Client, Body(shared, funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await guest.Client.GetAsync("/api/budget/expenses", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var forKid = await CreatedAsync(h.Owner, Body(kidEnvelope));
        var detail = await GetAsync(h.Owner, forKid.ExpenseId);
        detail.AddedByPersonId.ShouldBe(h.OwnerPersonId);
        detail.PaidByPersonId.ShouldBe(kid);
    }

    /// <summary>A retry with the same key creates one expense and returns the original result with 200.</summary>
    [Fact]
    public async Task Retry_ReturnsOriginalResult_AndCreatesOneExpense()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var requestId = Guid.NewGuid();
        var body = Body(account, funding: "HouseholdFunds", requestId: requestId);

        var first = await CreatedAsync(h.Owner, body);
        var retry = await PostAsync(h.Owner, body);

        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        var replay = (await retry.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
        replay.ExpenseId.ShouldBe(first.ExpenseId);
        replay.Revision.ShouldBe(1);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(1);
    }

    /// <summary>A reused key with a different payload is a 409 and changes nothing.</summary>
    [Fact]
    public async Task Retry_WithDifferentPayload_Returns409()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var requestId = Guid.NewGuid();
        await CreatedAsync(h.Owner, Body(account, "10.00", funding: "HouseholdFunds", requestId: requestId));

        (await PostAsync(h.Owner, Body(account, "10.01", funding: "HouseholdFunds", requestId: requestId))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await PostAsync(h.Owner, Body(account, "10.00", category: "Other", funding: "HouseholdFunds", requestId: requestId))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(1);
    }

    /// <summary>The same key from another actor is independent: no reuse and no exposure of the first actor's result.</summary>
    [Fact]
    public async Task SameKeyFromAnotherActor_CreatesItsOwnExpense()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var requestId = Guid.NewGuid();
        var body = Body(account, funding: "HouseholdFunds", requestId: requestId);

        var a = await CreatedAsync(h.Owner, body);
        var b = await CreatedAsync(bea.Client, body);

        b.ExpenseId.ShouldNotBe(a.ExpenseId);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(2);
    }

    /// <summary>Simultaneous identical submissions leave exactly one expense, one creation revision, and one result.</summary>
    [Fact]
    public async Task ConcurrentRetries_CreateOneExpense()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var body = Body(account, funding: "HouseholdFunds");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => PostAsync(h.Owner, body)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var ids = new HashSet<Guid>();
        foreach (var r in responses)
            ids.Add((await r.Content.ReadFromJsonAsync<ResultBody>(Ct))!.ExpenseId);
        ids.Count.ShouldBe(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var expenseId = global::Budget.Domain.ValueObjects.ExpenseId.From(ids.Single());
        (await db.ExpenseRevisions.CountAsync(r => r.ExpenseId == expenseId, Ct)).ShouldBe(1);
    }

    /// <summary>A retry still returns the original result after the recorder's role later fails current-roster checks — it is recognised first.</summary>
    [Fact]
    public async Task Retry_IsRecognisedBeforeRosterValidation()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var body = Body(account, paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId]);
        var first = await CreatedAsync(h.Owner, body);

        (await h.Owner.DeleteAsync($"/api/households/{h.Id}/members/{bea.PersonId}", Ct)).EnsureSuccessStatusCode();
        var retry = await PostAsync(h.Owner, body);

        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await retry.Content.ReadFromJsonAsync<ResultBody>(Ct))!.ExpenseId.ShouldBe(first.ExpenseId);
        (await PostAsync(h.Owner, Body(account, paidBy: bea.PersonId, participants: [h.OwnerPersonId]))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>Removing a participant later does not rewrite stored shares; the former member keeps their snapshot name.</summary>
    [Fact]
    public async Task MembershipChange_DoesNotRewriteShares()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var created = await CreatedAsync(h.Owner, Body(account, "10.01", paidBy: h.OwnerPersonId, participants: [h.OwnerPersonId, bea.PersonId]));
        var before = await GetAsync(h.Owner, created.ExpenseId);

        (await h.Owner.DeleteAsync($"/api/households/{h.Id}/members/{bea.PersonId}", Ct)).EnsureSuccessStatusCode();
        var after = await GetAsync(h.Owner, created.ExpenseId);

        after.Shares.Select(s => (s.PersonId, s.Amount)).ShouldBe(before.Shares.Select(s => (s.PersonId, s.Amount)));
        after.Shares.ShouldContain(s => s.PersonId == bea.PersonId && s.PersonDisplayName.Length > 0);
    }

    /// <summary>Exact amount + category + ±2-day window + excludeId returns only matching visible entries.</summary>
    [Fact]
    public async Task DuplicateHintFilters_MatchExactAmountCategoryAndDateWindow()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var keep = await CreatedAsync(h.Owner, Body(account, "19.99", "2026-10-03", "Groceries", "HouseholdFunds"));
        var edge = await CreatedAsync(h.Owner, Body(account, "19.99", "2026-10-05", "Groceries", "HouseholdFunds"));
        await CreatedAsync(h.Owner, Body(account, "19.99", "2026-10-06", "Groceries", "HouseholdFunds"));  // 3 days out
        await CreatedAsync(h.Owner, Body(account, "19.99", "2026-10-03", "Leisure", "HouseholdFunds"));    // category
        await CreatedAsync(h.Owner, Body(account, "19.98", "2026-10-03", "Groceries", "HouseholdFunds"));  // amount
        const string window = "?amount=19.99&category=Groceries&from=2026-10-01&to=2026-10-05&pageSize=5";

        var hits = await ListAsync(h.Owner, window);
        var excluding = await ListAsync(h.Owner, window + $"&excludeId={keep.ExpenseId}");

        hits.Items.Select(i => i.Id).Order().ShouldBe(new[] { keep.ExpenseId, edge.ExpenseId }.Order());
        hits.TotalCount.ShouldBe(2);
        excluding.Items.Select(i => i.Id).ShouldBe([edge.ExpenseId]);
    }

    /// <summary>Invalid filters are 400; page size is capped; ordering is newest purchase first and stable across pages.</summary>
    [Fact]
    public async Task List_ValidatesFilters_AndPaginatesNewestFirst()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        foreach (var day in new[] { "2026-09-01", "2026-09-03", "2026-09-02" })
            await CreatedAsync(h.Owner, Body(account, "5", day, funding: "HouseholdFunds"));

        var first = await ListAsync(h.Owner, "?page=1&pageSize=2");
        var second = await ListAsync(h.Owner, "?page=2&pageSize=2");
        var huge = await ListAsync(h.Owner, "?pageSize=9999");

        first.Items.Select(i => i.OccurredOn).ShouldBe(["2026-09-03", "2026-09-02"]);
        second.Items.Select(i => i.OccurredOn).ShouldBe(["2026-09-01"]);
        first.TotalCount.ShouldBe(3);
        huge.PageSize.ShouldBe(100);
        (await h.Owner.GetAsync("/api/budget/expenses?amount=abc", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await h.Owner.GetAsync("/api/budget/expenses?category=Nope", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await h.Owner.GetAsync("/api/budget/expenses?from=2026-10-05&to=2026-10-01", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>Another household never sees these expenses.</summary>
    [Fact]
    public async Task OtherHousehold_CannotSeeExpenses()
    {
        var a = await _scenario.NewHouseholdAsync();
        var b = await _scenario.NewHouseholdAsync();
        var created = await CreatedAsync(a.Owner, Body(await BudgetScenario.DefaultEnvelopeAsync(a.Owner), funding: "HouseholdFunds"));

        (await ListAsync(b.Owner)).TotalCount.ShouldBe(0);
        (await b.Owner.GetAsync($"/api/budget/expenses/{created.ExpenseId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private sealed record IdBody(Guid Id);
    private sealed record ResultBody(Guid ExpenseId, int Revision);
    private sealed record ShareBody(Guid PersonId, string PersonDisplayName, string Amount);
    private sealed record ExpenseBody(
        Guid Id, string Amount, string OccurredOn, Guid? PaidByPersonId, Guid AddedByPersonId, List<ShareBody> Shares);
    private sealed record PageBody(List<ExpenseBody> Items, int TotalCount, int Page, int PageSize);
}
