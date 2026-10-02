namespace Budget.IntegrationTests.Api;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#454 — outstanding balances and suggestions over HTTP to PostgreSQL.</summary>
public sealed class BudgetSettlementTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetSettlementTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task SharedAsync(
        HttpClient client, Guid account, string amount, Guid payer, Guid[] participants, string date = "2026-10-01")
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount,
            occurredOn = date,
            category = "Groceries",
            fundingSource = "Individual",
            paidByPersonId = payer,
            participantIds = participants,
        }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
    }

    private static async Task HouseholdFundsAsync(HttpClient client, Guid account, string amount)
        => (await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount,
            occurredOn = "2026-10-01",
            category = "Other",
            fundingSource = "HouseholdFunds",
        }, Ct)).EnsureSuccessStatusCode();

    private static async Task<SettlementBody> SettlementAsync(HttpClient client)
        => (await client.GetFromJsonAsync<SettlementBody>("/api/budget/settlement", Ct))!;

    private static decimal Net(SettlementBody s, Guid person)
        => decimal.Parse(s.Balances.Single(b => b.PersonId == person).Net, CultureInfo.InvariantCulture);

    private async Task<(BudgetScenario.Household H, BudgetScenario.Member Bea, Guid Account)> TwoAdultsAsync()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        return (h, bea, await BudgetScenario.DefaultEnvelopeAsync(h.Owner));
    }

    /// <summary>The design example before repayments: +40.00 / -40.00, and private/household-funded rows change nothing.</summary>
    [Fact]
    public async Task DesignExample_BeforeRepayments_IsReproduced()
    {
        var (h, bea, account) = await TwoAdultsAsync();
        Guid[] both = [h.OwnerPersonId, bea.PersonId];
        await SharedAsync(h.Owner, account, "120.00", h.OwnerPersonId, both);
        await SharedAsync(bea.Client, account, "40.00", bea.PersonId, both);
        await HouseholdFundsAsync(h.Owner, account, "80.00");
        var personal = (await (await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
        (await h.Owner.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = personal,
            amount = "30.00",
            occurredOn = "2026-10-01",
            category = "Leisure",
        }, Ct)).EnsureSuccessStatusCode();

        var s = await SettlementAsync(h.Owner);

        Net(s, h.OwnerPersonId).ShouldBe(40m);
        Net(s, bea.PersonId).ShouldBe(-40m);
        s.Balances.Sum(b => decimal.Parse(b.Net, CultureInfo.InvariantCulture)).ShouldBe(0m);
        s.IsSettled.ShouldBeFalse();
        s.Currency.ShouldBe("PLN");
        var transfer = s.Suggestions.Single();
        (transfer.FromPersonId, transfer.ToPersonId, transfer.Amount).ShouldBe((bea.PersonId, h.OwnerPersonId, "40.00"));
    }

    /// <summary>Future-dated entries count immediately; there is no date cutoff of any kind.</summary>
    [Fact]
    public async Task FutureDatedEntries_CountImmediately()
    {
        var (h, bea, account) = await TwoAdultsAsync();
        await SharedAsync(h.Owner, account, "10.00", h.OwnerPersonId, [h.OwnerPersonId, bea.PersonId], "2999-12-31");
        await SharedAsync(h.Owner, account, "20.00", h.OwnerPersonId, [h.OwnerPersonId, bea.PersonId], "2000-01-01");

        var s = await SettlementAsync(h.Owner);

        Net(s, bea.PersonId).ShouldBe(-15m);
    }

    /// <summary>Voided expenses contribute nothing; with nothing outstanding the household is settled with no suggestions.</summary>
    [Fact]
    public async Task VoidedExpenses_AreIgnored_AndEmptyIsSettled()
    {
        var (h, bea, account) = await TwoAdultsAsync();
        (await SettlementAsync(h.Owner)).IsSettled.ShouldBeTrue();

        var created = await h.Owner.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount = "50.00",
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = "Individual",
            paidByPersonId = h.OwnerPersonId,
            participantIds = new[] { h.OwnerPersonId, bea.PersonId },
        }, Ct);
        var id = (await created.Content.ReadFromJsonAsync<ResultBody>(Ct))!.ExpenseId;
        (await SettlementAsync(h.Owner)).IsSettled.ShouldBeFalse();

        (await h.Owner.PostAsJsonAsync($"/api/budget/expenses/{id}/void", new { clientRequestId = Guid.NewGuid(), expectedRevision = 1, reason = "oops" }, Ct))
            .EnsureSuccessStatusCode();

        var s = await SettlementAsync(h.Owner);
        s.IsSettled.ShouldBeTrue();
        s.Suggestions.ShouldBeEmpty();
    }

    /// <summary>Uneven cents stay exact: 100.00 over three people, applying suggestions zeroes everyone.</summary>
    [Fact]
    public async Task UnevenCents_AndSuggestionsSettleEveryone()
    {
        var (h, bea, account) = await TwoAdultsAsync();
        var cy = await _scenario.MemberAsync(h, "Adult");
        await SharedAsync(h.Owner, account, "100.00", h.OwnerPersonId, [h.OwnerPersonId, bea.PersonId, cy.PersonId]);

        var s = await SettlementAsync(h.Owner);

        s.Balances.Sum(b => decimal.Parse(b.Net, CultureInfo.InvariantCulture)).ShouldBe(0m);
        var net = s.Balances.ToDictionary(b => b.PersonId, b => decimal.Parse(b.Net, CultureInfo.InvariantCulture));
        foreach (var t in s.Suggestions)
        {
            var amount = decimal.Parse(t.Amount, CultureInfo.InvariantCulture);
            net[t.FromPersonId] += amount;
            net[t.ToPersonId] -= amount;
        }

        net.Values.ShouldAllBe(v => v == 0m);
    }

    /// <summary>A departed or demoted adult keeps their balance, flagged as a former adult, with their stored name.</summary>
    [Fact]
    public async Task DepartedOrDemotedParticipants_RetainBalances()
    {
        var (h, bea, account) = await TwoAdultsAsync();
        var cy = await _scenario.MemberAsync(h, "Adult");
        await SharedAsync(h.Owner, account, "90.00", h.OwnerPersonId, [h.OwnerPersonId, bea.PersonId, cy.PersonId]);

        (await h.Owner.DeleteAsync($"/api/households/{h.Id}/members/{bea.PersonId}", Ct)).EnsureSuccessStatusCode();
        (await h.Owner.PutAsJsonAsync($"/api/households/{h.Id}/members/{cy.PersonId}/role", new { role = "Child" }, Ct)).EnsureSuccessStatusCode();
        var s = await SettlementAsync(h.Owner);

        Net(s, bea.PersonId).ShouldBe(-30m);
        Net(s, cy.PersonId).ShouldBe(-30m);
        s.Balances.Single(b => b.PersonId == bea.PersonId).IsFormerAdult.ShouldBeTrue();
        s.Balances.Single(b => b.PersonId == cy.PersonId).IsFormerAdult.ShouldBeTrue();
        s.Balances.Single(b => b.PersonId == h.OwnerPersonId).IsFormerAdult.ShouldBeFalse();
        s.Balances.ShouldAllBe(b => b.DisplayName.Length > 0);
        s.Suggestions.Count.ShouldBe(2);
    }

    /// <summary>Children and guests cannot query settlement; another household sees none of it.</summary>
    [Fact]
    public async Task OnlyAdults_AndOnlyTheirHousehold()
    {
        var (h, bea, account) = await TwoAdultsAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var guest = await _scenario.MemberAsync(h, "Guest");
        var other = await _scenario.NewHouseholdAsync();
        await SharedAsync(h.Owner, account, "10.00", h.OwnerPersonId, [h.OwnerPersonId, bea.PersonId]);

        (await child.Client.GetAsync("/api/budget/settlement", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await guest.Client.GetAsync("/api/budget/settlement", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await SettlementAsync(bea.Client)).Balances.Count.ShouldBe(2);
        (await SettlementAsync(other.Owner)).Balances.ShouldBeEmpty();
    }

    /// <summary>A household without a budget has no settlement (404), not an empty ledger.</summary>
    [Fact]
    public async Task WithoutBudget_Returns404()
    {
        var h = await _scenario.NewHouseholdAsync(initialiseBudget: false);

        (await h.Owner.GetAsync("/api/budget/settlement", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private sealed record IdBody(Guid Id);
    private sealed record ResultBody(Guid ExpenseId, int Revision);
    private sealed record BalanceBody(Guid PersonId, string DisplayName, bool IsFormerAdult, string Net);
    private sealed record TransferBody(Guid FromPersonId, string FromDisplayName, Guid ToPersonId, string ToDisplayName, string Amount);
    private sealed record SettlementBody(string Currency, bool IsSettled, List<BalanceBody> Balances, List<TransferBody> Suggestions);
}
