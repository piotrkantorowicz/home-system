namespace Investments.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using global::Investments.Domain.ValueObjects;
using Investments.IntegrationTests.Infrastructure;

/// <summary>#577 — net worth and its month-end history: scopes, private-wallet exclusion, archive, incomplete, validation.</summary>
public sealed class NetWorthTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public NetWorthTests(InvestmentsDatabaseFixture fixture)
    {
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record KindTotal(string Kind, string Total);

    private sealed record OwnerTotal(Guid PersonId, string Name, string Total);

    private sealed record WalletValue(Guid Id, string Name, string? Balance, string? BalancePln, bool RateMissing);

    private sealed record NetWorth(string Total, bool Incomplete, List<KindTotal> ByKind, List<OwnerTotal> ByOwner, List<WalletValue> Wallets);

    private sealed record Point(DateOnly Date, string Total, bool Incomplete);

    private sealed record History(List<Point> Points);

    private static async Task<Guid> WalletWithBalanceAsync(
        HttpClient client, string name, string amount, string visibility = "Shared", string kind = "Brokerage",
        string currency = "PLN", string asOf = "2026-09-01", Guid? ownerPersonId = null)
    {
        var wallet = await InvestmentsScenario.CreateWalletAsync(client, name, visibility, ownerPersonId, kind, currency);
        (await client.PutAsJsonAsync($"/api/investments/wallets/{wallet.Id}/snapshots/{asOf}", new { amount }, Ct))
            .EnsureSuccessStatusCode();
        return wallet.Id;
    }

    private static async Task<NetWorth> NetWorthAsync(HttpClient client, string query = "")
        => (await client.GetFromJsonAsync<NetWorth>($"/api/investments/net-worth{query}", Ct))!;

    /// <summary>
    /// Household = shared + own private (+ managed); Mine = own (+ managed). Another adult's private wallet never
    /// shows in totals, breakdowns or the wallet list — and each adult sees a different household total.
    /// </summary>
    [Fact]
    public async Task Scopes_ExcludeAnotherAdultsPrivateWallet()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        await WalletWithBalanceAsync(h.Owner, "Joint", "1000", kind: "Savings");
        await WalletWithBalanceAsync(h.Owner, "Owner IKE", "500", "Private", "IKE");
        var hidden = await WalletWithBalanceAsync(adult.Client, "Adult secret", "700", "Private");
        await WalletWithBalanceAsync(adult.Client, "Adult shared", "300");

        var ownerHousehold = await NetWorthAsync(h.Owner);
        var ownerMine = await NetWorthAsync(h.Owner, "?scope=mine");
        var adultHousehold = await NetWorthAsync(adult.Client);

        ownerHousehold.Total.ShouldBe("1800.00");
        ownerHousehold.Wallets.ShouldNotContain(w => w.Id == hidden);
        ownerHousehold.ByOwner.Select(o => (o.PersonId, o.Total)).ShouldBe([(h.OwnerPersonId, "1500.00"), (adult.PersonId, "300.00")]);
        ownerHousehold.ByKind.Select(k => (k.Kind, k.Total)).ShouldBe([("Savings", "1000.00"), ("IKE", "500.00"), ("Brokerage", "300.00")]);
        ownerMine.Total.ShouldBe("1500.00");
        adultHousehold.Total.ShouldBe("2000.00");
    }

    /// <summary>Mine covers the caller's managed members (kids) too, not another adult's shared wallet.</summary>
    [Fact]
    public async Task Mine_IncludesManagedMembers()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var kid = await InvestmentsScenario.ManagedChildAsync(h);
        await WalletWithBalanceAsync(h.Owner, "Owner IKE", "500", "Private", "IKE");
        await WalletWithBalanceAsync(h.Owner, "Kid PPK", "200", "Private", "PPK", ownerPersonId: kid);
        await WalletWithBalanceAsync(adult.Client, "Adult shared", "300");

        var ownerMine = await NetWorthAsync(h.Owner, "?scope=mine");
        var adultMine = await NetWorthAsync(adult.Client, "?scope=mine");

        ownerMine.Total.ShouldBe("700.00");
        adultMine.Total.ShouldBe("500.00");
    }

    /// <summary>An archived wallet no longer counts from its archive date, but history before it keeps it.</summary>
    [Fact]
    public async Task ArchivedWallet_ExcludedFromArchiveDate()
    {
        var h = await _scenario.NewHouseholdAsync();
        await WalletWithBalanceAsync(h.Owner, "Active", "100");
        var old = await WalletWithBalanceAsync(h.Owner, "Closed", "900", asOf: "2026-08-01");
        (await h.Owner.PostAsync($"/api/investments/wallets/{old}/archive", null, Ct)).EnsureSuccessStatusCode();

        var netWorth = await NetWorthAsync(h.Owner);
        var history = await h.Owner.GetFromJsonAsync<History>("/api/investments/net-worth/history?from=2026-08-01&to=2026-09-12", Ct);

        netWorth.Total.ShouldBe("100.00");
        netWorth.Wallets.ShouldHaveSingleItem().Name.ShouldBe("Active");
        history!.Points.Select(p => (p.Date, p.Total)).ShouldBe(
            [(new DateOnly(2026, 8, 31), "900.00"), (new DateOnly(2026, 9, 12), "100.00")]);
    }

    /// <summary>
    /// The net-worth figure converts at today's rate even for a past date; the history chart keeps
    /// each month end's rate. USD only — rates are shared across this class and the missing-rate test uses EUR.
    /// </summary>
    [Fact]
    public async Task NetWorth_UsesTodaysRate_HistoryKeepsMonthEndRate()
    {
        _factory.Nbp.Publish(WalletCurrency.USD, new DateOnly(2026, 6, 30), 4.0m);
        _factory.Nbp.Publish(WalletCurrency.USD, new DateOnly(2026, 9, 11), 4.5m);
        var h = await _scenario.NewHouseholdAsync();
        await WalletWithBalanceAsync(h.Owner, "IBKR", "100", currency: "USD", asOf: "2026-06-15");

        var netWorth = await NetWorthAsync(h.Owner, "?asOf=2026-06-30");
        var history = await h.Owner.GetFromJsonAsync<History>("/api/investments/net-worth/history?from=2026-06-01&to=2026-06-30", Ct);

        netWorth.Total.ShouldBe("450.00");
        history!.Points.ShouldHaveSingleItem().Total.ShouldBe("400.00");
    }

    /// <summary>A foreign balance without a rate is left out of the total and flagged.</summary>
    [Fact]
    public async Task MissingRate_ExcludedAndIncomplete()
    {
        var h = await _scenario.NewHouseholdAsync();
        await WalletWithBalanceAsync(h.Owner, "PLN", "100");
        var eur = await WalletWithBalanceAsync(h.Owner, "No rate", "50", currency: "EUR", asOf: "2019-05-06");

        var netWorth = await NetWorthAsync(h.Owner, "?asOf=2026-09-12");

        netWorth.Total.ShouldBe("100.00");
        netWorth.Incomplete.ShouldBeTrue();
        netWorth.Wallets.Single(w => w.Id == eur).RateMissing.ShouldBeTrue();
    }

    /// <summary>One point per month end, plus the last date of the current month.</summary>
    [Fact]
    public async Task History_MonthEndsPlusToday()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await WalletWithBalanceAsync(h.Owner, "XTB", "100", asOf: "2026-07-15");
        (await h.Owner.PutAsJsonAsync($"/api/investments/wallets/{wallet}/snapshots/2026-08-31", new { amount = "200" }, Ct))
            .EnsureSuccessStatusCode();

        var history = await h.Owner.GetFromJsonAsync<History>("/api/investments/net-worth/history?from=2026-06-01&to=2026-09-12", Ct);

        history!.Points.Select(p => (p.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), p.Total))
            .ShouldBe([("2026-06-30", "0.00"), ("2026-07-31", "100.00"), ("2026-08-31", "200.00"), ("2026-09-12", "200.00")]);
    }

    /// <summary>Bad scope, inverted, over-long or future ranges are rejected.</summary>
    /// <param name="path">Request path and query.</param>
    [Theory]
    [InlineData("/api/investments/net-worth?scope=everyone")]
    [InlineData("/api/investments/net-worth?asOf=2026-09-13")]
    [InlineData("/api/investments/net-worth/history?from=2026-09-01&to=2026-08-01")]
    [InlineData("/api/investments/net-worth/history?from=2016-01-01&to=2026-09-01")]
    [InlineData("/api/investments/net-worth/history?to=2026-10-01")]
    public async Task InvalidInput_Returns400(string path)
    {
        var h = await _scenario.NewHouseholdAsync();

        var response = await h.Owner.GetAsync(path, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>A guest has no access.</summary>
    [Fact]
    public async Task Guest_Returns403()
    {
        var h = await _scenario.NewHouseholdAsync();
        var guest = await _scenario.MemberAsync(h, "Guest");

        var response = await guest.Client.GetAsync("/api/investments/net-worth", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
