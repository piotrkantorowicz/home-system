namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#453 — monthly summary and limits: exact aggregates, visibility and concurrency over HTTP to PostgreSQL.</summary>
public sealed class BudgetSummaryTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetSummaryTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<ResultBody> SpendAsync(
        HttpClient client, Guid account, string amount, string date, string category = "Groceries")
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount,
            occurredOn = date,
            category,
            fundingSource = "HouseholdFunds",
        }, Ct);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            // Personal envelopes are individually funded by their owner.
            response = await client.PostAsJsonAsync("/api/budget/expenses", new
            {
                clientRequestId = Guid.NewGuid(),
                accountId = account,
                amount,
                occurredOn = date,
                category,
            }, Ct);
        }

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
    }

    private static async Task<SummaryBody> SummaryAsync(HttpClient client, string month, string scope = "shared", Guid? owner = null)
        => (await client.GetFromJsonAsync<SummaryBody>(
            $"/api/budget/summary?month={month}&scope={scope}{(owner is null ? "" : $"&ownerPersonId={owner}")}", Ct))!;

    private static Task<HttpResponseMessage> SetLimitAsync(HttpClient client, Guid account, string month, string amount, int? expected = null)
        => client.PutAsJsonAsync($"/api/budget/accounts/{account}/limits/{month}", new { amount, expectedRevision = expected }, Ct);

    private static async Task<Guid> PersonalEnvelopeAsync(HttpClient client, Guid? owner = null)
    {
        var response = await client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal", ownerPersonId = owner }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
    }

    /// <summary>A missing limit and a zero limit are different: no remaining vs overspent by any spending.</summary>
    [Fact]
    public async Task MissingAndZeroLimitsDiffer()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await SpendAsync(h.Owner, account, "5.00", "2026-10-10");

        var none = (await SummaryAsync(h.Owner, "2026-10")).Envelopes.Single();
        none.Limit.ShouldBeNull();
        none.Remaining.ShouldBeNull();
        none.IsOverspent.ShouldBeFalse();

        (await SetLimitAsync(h.Owner, account, "2026-10", "0.00")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var zero = (await SummaryAsync(h.Owner, "2026-10")).Envelopes.Single();
        zero.Limit.ShouldBe("0.00");
        zero.Remaining.ShouldBe("-5.00");
        zero.IsOverspent.ShouldBeTrue();
    }

    /// <summary>Overspending is reported but never blocks recording another expense.</summary>
    [Fact]
    public async Task Overspending_NeverBlocksExpenses()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await SetLimitAsync(h.Owner, account, "2026-10", "10.00");

        await SpendAsync(h.Owner, account, "9.99", "2026-10-01");
        await SpendAsync(h.Owner, account, "0.02", "2026-10-02");
        await SpendAsync(h.Owner, account, "50.00", "2026-10-03");

        var row = (await SummaryAsync(h.Owner, "2026-10")).Envelopes.Single();
        row.Spent.ShouldBe("60.01");
        row.Remaining.ShouldBe("-50.01");
        row.IsOverspent.ShouldBeTrue();
    }

    /// <summary>Totals are exact to the cent, by envelope and category.</summary>
    [Fact]
    public async Task Totals_AreExact_ByEnvelopeAndCategory()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        for (var i = 0; i < 10; i++)
            await SpendAsync(h.Owner, account, "0.10", "2026-10-05");
        await SpendAsync(h.Owner, account, "0.01", "2026-10-06", "Health");

        var summary = await SummaryAsync(h.Owner, "2026-10");

        summary.TotalSpent.ShouldBe("1.01");
        summary.Envelopes.Single().Spent.ShouldBe("1.01");
        summary.Categories.Select(c => (c.Category, c.Spent)).ShouldBe([("Groceries", "1.00"), ("Health", "0.01")]);
        summary.Currency.ShouldBe("PLN");
    }

    /// <summary>Months are half-open on the purchase date, across year and leap-day boundaries.</summary>
    [Fact]
    public async Task MonthBoundaries_HandleYearAndLeapDay()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await SpendAsync(h.Owner, account, "1.00", "2026-12-31");
        await SpendAsync(h.Owner, account, "2.00", "2027-01-01");
        await SpendAsync(h.Owner, account, "3.00", "2028-02-29");
        await SpendAsync(h.Owner, account, "4.00", "2028-03-01");
        await SpendAsync(h.Owner, account, "5.00", "2026-03-01");

        (await SummaryAsync(h.Owner, "2026-12")).TotalSpent.ShouldBe("1.00");
        (await SummaryAsync(h.Owner, "2027-01")).TotalSpent.ShouldBe("2.00");
        (await SummaryAsync(h.Owner, "2028-02")).TotalSpent.ShouldBe("3.00");
        (await SummaryAsync(h.Owner, "2028-03")).TotalSpent.ShouldBe("4.00");
        (await SummaryAsync(h.Owner, "2026-02")).TotalSpent.ShouldBe("0.00");
        (await SummaryAsync(h.Owner, "2026-03")).TotalSpent.ShouldBe("5.00");
    }

    /// <summary>Correcting the purchase date moves spending to that month; voiding removes it.</summary>
    [Fact]
    public async Task Corrections_AffectThePurchaseMonth_AndVoidsDropOut()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var expense = await SpendAsync(h.Owner, account, "20.00", "2026-09-30");

        var edit = await h.Owner.PutAsJsonAsync($"/api/budget/expenses/{expense.ExpenseId}", new
        {
            clientRequestId = Guid.NewGuid(),
            expectedRevision = 1,
            reason = "wrong day",
            amount = "20.00",
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = "HouseholdFunds",
        }, Ct);
        edit.EnsureSuccessStatusCode();

        (await SummaryAsync(h.Owner, "2026-09")).TotalSpent.ShouldBe("0.00");
        (await SummaryAsync(h.Owner, "2026-10")).TotalSpent.ShouldBe("20.00");

        (await h.Owner.PostAsJsonAsync($"/api/budget/expenses/{expense.ExpenseId}/void",
            new { clientRequestId = Guid.NewGuid(), expectedRevision = 2, reason = "oops" }, Ct)).EnsureSuccessStatusCode();
        (await SummaryAsync(h.Owner, "2026-10")).TotalSpent.ShouldBe("0.00");
    }

    /// <summary>Private spending never enters Shared totals; the personal scope sums only that person's envelopes.</summary>
    [Fact]
    public async Task PersonalSpending_IsExcludedFromShared_AndPrivateToOwner()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var ownerPersonal = await PersonalEnvelopeAsync(h.Owner);
        var beaPersonal = await PersonalEnvelopeAsync(bea.Client);
        await SpendAsync(h.Owner, shared, "10.00", "2026-10-02");
        await SpendAsync(h.Owner, ownerPersonal, "7.00", "2026-10-02");
        await SpendAsync(bea.Client, beaPersonal, "3.00", "2026-10-02");

        (await SummaryAsync(h.Owner, "2026-10")).TotalSpent.ShouldBe("10.00");
        (await SummaryAsync(bea.Client, "2026-10")).TotalSpent.ShouldBe("10.00");
        (await SummaryAsync(h.Owner, "2026-10", "personal")).TotalSpent.ShouldBe("7.00");
        (await SummaryAsync(bea.Client, "2026-10", "personal")).TotalSpent.ShouldBe("3.00");

        // Another adult's personal scope, even by owner, is denied; nothing leaks.
        (await h.Owner.GetAsync($"/api/budget/summary?month=2026-10&scope=personal&ownerPersonId={bea.PersonId}", Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>An adult can summarise a managed member's personal envelopes; the child cannot see shared spending.</summary>
    [Fact]
    public async Task ManagedMemberScope_AndChildRestrictions()
    {
        var h = await _scenario.NewHouseholdAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var guest = await _scenario.MemberAsync(h, "Guest");
        var kid = await BudgetScenario.ManagedChildAsync(h);
        var kidEnvelope = await PersonalEnvelopeAsync(h.Owner, kid);
        var childEnvelope = await PersonalEnvelopeAsync(child.Client);
        await SpendAsync(h.Owner, kidEnvelope, "4.00", "2026-10-02");
        await SpendAsync(child.Client, childEnvelope, "2.00", "2026-10-02");

        (await SummaryAsync(h.Owner, "2026-10", "personal", kid)).TotalSpent.ShouldBe("4.00");
        (await SummaryAsync(child.Client, "2026-10", "personal")).TotalSpent.ShouldBe("2.00");
        (await child.Client.GetAsync("/api/budget/summary?month=2026-10&scope=shared", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await guest.Client.GetAsync("/api/budget/summary?month=2026-10&scope=shared", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await child.Client.GetAsync($"/api/budget/summary?month=2026-10&scope=personal&ownerPersonId={kid}", Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Invalid month or scope is a 400; an uninitialised budget is a 404.</summary>
    [Theory]
    [InlineData("2026-13", "shared")]
    [InlineData("2026-1", "shared")]
    [InlineData("", "shared")]
    [InlineData("2026-10", "everyone")]
    [InlineData("2026-10", "")]
    public async Task InvalidMonthOrScope_Returns400(string month, string scope)
    {
        var h = await _scenario.NewHouseholdAsync();

        (await h.Owner.GetAsync($"/api/budget/summary?month={month}&scope={scope}", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        if (month != "2026-10")
            (await h.Owner.GetAsync($"/api/budget/limits?month={month}", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>Archived envelopes appear in a month only when they have spending or a limit.</summary>
    [Fact]
    public async Task ArchivedEnvelope_AppearsOnlyWithActivity()
    {
        var h = await _scenario.NewHouseholdAsync();
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var old = (await (await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "Old", visibility = "Household" }, Ct))
            .Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
        await SpendAsync(h.Owner, old, "1.00", "2026-10-02");
        (await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{old}/archive", new { expectedRevision = 1 }, Ct)).EnsureSuccessStatusCode();

        (await SummaryAsync(h.Owner, "2026-10")).Envelopes.Select(e => e.Name).ShouldBe(["Everyday", "Old"]);
        (await SummaryAsync(h.Owner, "2026-09")).Envelopes.Select(e => e.Name).ShouldBe(["Everyday"]);
        shared.ShouldNotBe(Guid.Empty);
    }

    /// <summary>Limit lifecycle: set, change with the revision, stale is 409, clear, clear again is a no-op.</summary>
    [Fact]
    public async Task Limit_Lifecycle_WithRevisions()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        var set = (await (await SetLimitAsync(h.Owner, account, "2026-10", "100.00")).Content.ReadFromJsonAsync<LimitBody>(Ct))!;
        set.Revision.ShouldBe(1);
        (await SetLimitAsync(h.Owner, account, "2026-10", "120.00")).StatusCode.ShouldBe(HttpStatusCode.Conflict); // exists, no revision
        var changed = (await (await SetLimitAsync(h.Owner, account, "2026-10", "120.00", 1)).Content.ReadFromJsonAsync<LimitBody>(Ct))!;
        changed.Revision.ShouldBe(2);
        changed.Amount.ShouldBe("120.00");
        (await SetLimitAsync(h.Owner, account, "2026-10", "130.00", 1)).StatusCode.ShouldBe(HttpStatusCode.Conflict); // stale
        (await SetLimitAsync(h.Owner, account, "2026-11", "5.00", 3)).StatusCode.ShouldBe(HttpStatusCode.Conflict); // nothing to change

        var listed = (await h.Owner.GetFromJsonAsync<List<LimitBody>>("/api/budget/limits?month=2026-10", Ct))!;
        listed.Select(l => (l.Amount, l.Revision)).ShouldBe([("120.00", 2)]);
        (await h.Owner.GetFromJsonAsync<List<LimitBody>>("/api/budget/limits?month=2026-11", Ct))!.ShouldBeEmpty();

        (await h.Owner.DeleteAsync($"/api/budget/accounts/{account}/limits/2026-10?expectedRevision=1", Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await h.Owner.DeleteAsync($"/api/budget/accounts/{account}/limits/2026-10?expectedRevision=2", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await h.Owner.DeleteAsync($"/api/budget/accounts/{account}/limits/2026-10?expectedRevision=2", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await SummaryAsync(h.Owner, "2026-10")).Envelopes.Single().Limit.ShouldBeNull();
    }

    /// <summary>Simultaneous first-time sets: exactly one wins, the rest are 409, and one row remains.</summary>
    [Fact]
    public async Task ConcurrentFirstSets_OneSucceeds()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        var responses = await Task.WhenAll(Enumerable.Range(1, 6).Select(i => SetLimitAsync(h.Owner, account, "2026-10", $"{i}.00")));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(5);
        (await h.Owner.GetFromJsonAsync<List<LimitBody>>("/api/budget/limits?month=2026-10", Ct))!.Count.ShouldBe(1);
    }

    /// <summary>Two updates on one expected revision: one wins, one is 409.</summary>
    [Fact]
    public async Task ConcurrentUpdates_OneSucceeds()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await SetLimitAsync(h.Owner, account, "2026-10", "10.00");

        var responses = await Task.WhenAll(Enumerable.Range(1, 6).Select(i => SetLimitAsync(h.Owner, account, "2026-10", $"{i + 10}.00", 1)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(5);
    }

    /// <summary>Limits follow envelope privacy and archive rules, and bad amounts are rejected.</summary>
    [Fact]
    public async Task Limits_FollowPrivacyAndArchiveRules()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var child = await _scenario.MemberAsync(h, "Child");
        var other = await _scenario.NewHouseholdAsync();
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var beaPrivate = await PersonalEnvelopeAsync(bea.Client);
        var childOwn = await PersonalEnvelopeAsync(child.Client);

        (await SetLimitAsync(bea.Client, beaPrivate, "2026-10", "50.00")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await SetLimitAsync(h.Owner, beaPrivate, "2026-10", "1.00")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await h.Owner.GetFromJsonAsync<List<LimitBody>>("/api/budget/limits?month=2026-10", Ct))!.ShouldBeEmpty();
        (await SetLimitAsync(other.Owner, shared, "2026-10", "1.00")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SetLimitAsync(child.Client, shared, "2026-10", "1.00")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SetLimitAsync(child.Client, childOwn, "2026-10", "5.00")).StatusCode.ShouldBe(HttpStatusCode.OK);

        foreach (var bad in new[] { "-1", "1.005", "1e2", "" })
            (await SetLimitAsync(h.Owner, shared, "2026-10", bad)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SetLimitAsync(h.Owner, shared, "2026-13", "1.00")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{shared}/archive", new { expectedRevision = 1 }, Ct)).EnsureSuccessStatusCode();
        (await SetLimitAsync(h.Owner, shared, "2026-10", "9.00")).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record IdBody(Guid Id);
    private sealed record ResultBody(Guid ExpenseId, int Revision);
    private sealed record LimitBody(Guid AccountId, string Month, string Amount, int Revision);
    private sealed record EnvelopeBody(Guid AccountId, string Name, string Spent, string? Limit, int? LimitRevision, string? Remaining, bool IsOverspent);
    private sealed record CategoryBody(string Category, string Spent);
    private sealed record SummaryBody(string Month, string Scope, string Currency, string TotalSpent, List<EnvelopeBody> Envelopes, List<CategoryBody> Categories);
}
