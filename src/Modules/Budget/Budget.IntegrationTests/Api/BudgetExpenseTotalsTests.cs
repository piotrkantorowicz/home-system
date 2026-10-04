namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#489 — list aggregates cover the whole filter, never just the loaded page; monthly counts in the summary.</summary>
public sealed class BudgetExpenseTotalsTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetExpenseTotalsTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<Guid> CreateAsync(
        HttpClient client, Guid account, string amount, string date, string? description = null, Guid? paidBy = null, Guid[]? participants = null,
        bool personal = false)
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount,
            occurredOn = date,
            category = "Groceries",
            fundingSource = personal ? null : paidBy is null ? "HouseholdFunds" : "Individual",
            paidByPersonId = paidBy,
            participantIds = participants,
            description,
        }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!.ExpenseId;
    }

    private static async Task<ListBody> ListAsync(HttpClient client, string query = "")
        => (await client.GetFromJsonAsync<ListBody>($"/api/budget/expenses{query}", Ct))!;

    /// <summary>101 expenses over three days: totals, counts and day totals are exact while only one small page is loaded.</summary>
    [Fact]
    public async Task Totals_CoverEveryPage_WithExactCents()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        string[] days = ["2026-10-01", "2026-10-02", "2026-10-03"];
        for (var i = 0; i < 101; i++)
            await CreateAsync(h.Owner, account, "0.33", days[i % 3]);

        var page = await ListAsync(h.Owner, "?pageSize=5&page=2");

        page.Items.Count.ShouldBe(5);
        page.TotalCount.ShouldBe(101);
        page.ActiveCount.ShouldBe(101);
        page.TotalAmount.ShouldBe("33.33");
        page.YourShareAmount.ShouldBe("0.00");
        page.DailyTotals.Select(d => (d.Date, d.Total, d.Count))
            .ShouldBe([("2026-10-03", "10.89", 33), ("2026-10-02", "11.22", 34), ("2026-10-01", "11.22", 34)]);
    }

    /// <summary>Totals follow search and date filters, ignore voided rows even when listed, and count only the caller's own share.</summary>
    [Fact]
    public async Task Totals_FollowFilters_ExcludeVoided_AndSumCallerShare()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await CreateAsync(h.Owner, account, "10.00", "2026-10-01", "Lidl run");
        await CreateAsync(h.Owner, account, "5.00", "2026-09-30", "Lidl old");
        await CreateAsync(h.Owner, account, "7.00", "2026-10-01", "Bakery");
        await CreateAsync(h.Owner, account, "100.00", "2026-10-02", "Split dinner", bea.PersonId, [h.OwnerPersonId, bea.PersonId]);
        var voided = await CreateAsync(h.Owner, account, "50.00", "2026-10-01", "Lidl void");
        (await h.Owner.PostAsJsonAsync($"/api/budget/expenses/{voided}/void", new { clientRequestId = Guid.NewGuid(), expectedRevision = 1, reason = "mistake" }, Ct))
            .EnsureSuccessStatusCode();

        var lidl = await ListAsync(h.Owner, "?search=lidl&from=2026-10-01&to=2026-10-31&includeVoided=true");
        lidl.TotalCount.ShouldBe(2);
        lidl.ActiveCount.ShouldBe(1);
        lidl.TotalAmount.ShouldBe("10.00");

        var all = await ListAsync(h.Owner);
        all.TotalAmount.ShouldBe("122.00");
        (await ListAsync(bea.Client)).YourShareAmount.ShouldBe("50.00");
        all.YourShareAmount.ShouldBe("50.00");
    }

    /// <summary>Another member's personal envelope never leaks into totals, and a child sees none of the shared spending.</summary>
    [Fact]
    public async Task Totals_NeverLeakHiddenEnvelopes()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var child = await _scenario.MemberAsync(h, "Child");
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var personalId = (await (await bea.Client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
        await CreateAsync(h.Owner, shared, "12.00", "2026-10-01");
        await CreateAsync(bea.Client, personalId, "99.00", "2026-10-01", personal: true);

        var owner = await ListAsync(h.Owner);
        owner.TotalAmount.ShouldBe("12.00");
        owner.DailyTotals.Single().Count.ShouldBe(1);
        (await ListAsync(bea.Client)).TotalAmount.ShouldBe("111.00");
        var kid = await ListAsync(child.Client);
        kid.TotalCount.ShouldBe(0);
        kid.TotalAmount.ShouldBe("0.00");
        kid.DailyTotals.ShouldBeEmpty();
    }

    /// <summary>The monthly summary counts active expenses per envelope and overall; voided ones are left out.</summary>
    [Fact]
    public async Task Summary_CountsActiveExpensesPerMonth()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await CreateAsync(h.Owner, account, "10.00", "2026-10-01");
        await CreateAsync(h.Owner, account, "10.00", "2026-10-15");
        await CreateAsync(h.Owner, account, "10.00", "2026-09-30");
        var voided = await CreateAsync(h.Owner, account, "10.00", "2026-10-02");
        (await h.Owner.PostAsJsonAsync($"/api/budget/expenses/{voided}/void", new { clientRequestId = Guid.NewGuid(), expectedRevision = 1, reason = "mistake" }, Ct))
            .EnsureSuccessStatusCode();

        var summary = (await h.Owner.GetFromJsonAsync<SummaryBody>("/api/budget/summary?month=2026-10&scope=shared", Ct))!;

        summary.ExpenseCount.ShouldBe(2);
        summary.TotalSpent.ShouldBe("20.00");
        summary.Envelopes.Single(e => e.AccountId == account).ExpenseCount.ShouldBe(2);
    }

    private sealed record IdBody(Guid Id);
    private sealed record ResultBody(Guid ExpenseId, int Revision);
    private sealed record DayBody(string Date, string Total, int Count);
    private sealed record ListBody(List<object> Items, int TotalCount, int ActiveCount, string TotalAmount, string YourShareAmount, List<DayBody> DailyTotals);
    private sealed record EnvelopeBody(Guid AccountId, int ExpenseCount);
    private sealed record SummaryBody(string TotalSpent, int ExpenseCount, List<EnvelopeBody> Envelopes);
}
