namespace Investments.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using global::Investments.Domain.ValueObjects;
using global::Investments.Infrastructure.Fx;
using global::Investments.Infrastructure.Persistence;
using Investments.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// #576 — NBP rates: backfill on write, values at today's (newest) rate, NBP outages never failing a
/// write, and an idempotent worker. Rates are global, so each test starts with none stored
/// (tests in a class run one at a time).
/// </summary>
public sealed class FxTests : IClassFixture<InvestmentsDatabaseFixture>, IAsyncLifetime
{
    private readonly InvestmentsDatabaseFixture _fixture;
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public FxTests(InvestmentsDatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
    }

    /// <summary>Clears stored rates so each test sees only what its fake NBP publishes.</summary>
    public async ValueTask InitializeAsync()
    {
        await using var db = NewDbContext();
        await db.FxRates.ExecuteDeleteAsync(Ct);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    private InvestmentsDbContext NewDbContext()
        => new(new DbContextOptionsBuilder<InvestmentsDbContext>().UseNpgsql(_fixture.ConnectionString).Options);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record Rate(string Mid, DateOnly EffectiveDate, string TableNo);

    private sealed record WalletValue(string? LatestBalancePln, Rate? Rate, bool RateMissing);

    private sealed record SnapshotValue(DateOnly AsOf, string? AmountPln, Rate? Rate, bool RateMissing);

    private sealed record SnapshotPage(List<SnapshotValue> Items);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid walletId, string asOf, string amount)
        => client.PutAsJsonAsync($"/api/investments/wallets/{walletId}/snapshots/{asOf}", new { amount }, Ct);

    private static async Task<WalletValue> WalletAsync(HttpClient client, Guid id)
        => (await client.GetFromJsonAsync<WalletValue>($"/api/investments/wallets/{id}", Ct))!;

    private Task RunWorkerAsync() => _factory.Services.GetRequiredService<NbpFxWorker>().RunOnceAsync(Ct);

    /// <summary>When the newest rate is a Friday's, it applies over the weekend; it is fetched when the balance is recorded and the rate and table are exposed.</summary>
    [Fact]
    public async Task ForeignBalance_OnWeekend_UsesPriorBusinessDayRate()
    {
        _factory.Nbp.Publish(WalletCurrency.EUR, new DateOnly(2025, 1, 3), 4.2716m);
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Saxo", currency: "EUR");

        (await PutAsync(h.Owner, wallet.Id, "2025-01-05", "1000.50")).StatusCode.ShouldBe(HttpStatusCode.Created);
        var detail = await WalletAsync(h.Owner, wallet.Id);
        var history = await h.Owner.GetFromJsonAsync<SnapshotPage>($"/api/investments/wallets/{wallet.Id}/snapshots", Ct);

        detail.LatestBalancePln.ShouldBe("4273.74");
        detail.RateMissing.ShouldBeFalse();
        detail.Rate.ShouldBe(new Rate("4.271600", new DateOnly(2025, 1, 3), "003/A/NBP/2025"));
        var row = history!.Items.ShouldHaveSingleItem();
        row.AmountPln.ShouldBe("4273.74");
        row.Rate!.EffectiveDate.ShouldBe(new DateOnly(2025, 1, 3));
    }

    /// <summary>A PLN wallet needs no rate.</summary>
    [Fact]
    public async Task PlnBalance_HasNoRate()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "mBank savings");

        (await PutAsync(h.Owner, wallet.Id, "2026-09-01", "250")).EnsureSuccessStatusCode();
        var detail = await WalletAsync(h.Owner, wallet.Id);

        detail.LatestBalancePln.ShouldBe("250.00");
        detail.Rate.ShouldBeNull();
        detail.RateMissing.ShouldBeFalse();
    }

    /// <summary>With NBP down the write still succeeds and the value is flagged missing until the worker fills it.</summary>
    [Fact]
    public async Task NbpDown_WriteSucceeds_RateMissingUntilWorkerRuns()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "IBKR", currency: "USD");
        _factory.Nbp.Unavailable = true;

        var put = await PutAsync(h.Owner, wallet.Id, "2024-06-03", "100");
        var before = await WalletAsync(h.Owner, wallet.Id);

        _factory.Nbp.Unavailable = false;
        _factory.Nbp.Publish(WalletCurrency.USD, new DateOnly(2024, 6, 3), 3.9512m);
        await RunWorkerAsync();
        var after = await WalletAsync(h.Owner, wallet.Id);

        put.StatusCode.ShouldBe(HttpStatusCode.Created);
        before.RateMissing.ShouldBeTrue();
        before.LatestBalancePln.ShouldBeNull();
        after.RateMissing.ShouldBeFalse();
        after.LatestBalancePln.ShouldBe("395.12");
    }

    /// <summary>Wallet and history values use today's rate, not the rate of the balance's own date.</summary>
    [Fact]
    public async Task ForeignBalance_UsesTodaysRate()
    {
        _factory.Nbp.Publish(WalletCurrency.EUR, new DateOnly(2025, 1, 3), 4.2716m);
        _factory.Nbp.Publish(WalletCurrency.EUR, new DateOnly(2026, 9, 11), 4.3m);
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Old EUR", currency: "EUR");

        (await PutAsync(h.Owner, wallet.Id, "2025-01-05", "1000")).EnsureSuccessStatusCode();
        var detail = await WalletAsync(h.Owner, wallet.Id);
        var history = await h.Owner.GetFromJsonAsync<SnapshotPage>($"/api/investments/wallets/{wallet.Id}/snapshots", Ct);

        detail.LatestBalancePln.ShouldBe("4300.00");
        detail.Rate!.EffectiveDate.ShouldBe(new DateOnly(2026, 9, 11));
        var row = history!.Items.ShouldHaveSingleItem();
        row.AmountPln.ShouldBe("4300.00");
        row.Rate!.EffectiveDate.ShouldBe(new DateOnly(2026, 9, 11));
    }

    /// <summary>Running the worker again stores nothing twice.</summary>
    [Fact]
    public async Task Worker_IsIdempotent()
    {
        _factory.Nbp.Publish(WalletCurrency.EUR, new DateOnly(2023, 2, 27), 4.71m);
        _factory.Nbp.Publish(WalletCurrency.EUR, new DateOnly(2023, 2, 28), 4.72m);
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "EUR deposit", currency: "EUR");
        (await PutAsync(h.Owner, wallet.Id, "2023-03-01", "10")).EnsureSuccessStatusCode();

        await RunWorkerAsync();
        await RunWorkerAsync();

        await using var db = NewDbContext();
        var stored = await db.FxRates.CountAsync(
            r => r.Currency == WalletCurrency.EUR && r.EffectiveDate.Year == 2023, Ct);
        stored.ShouldBe(2);
    }

    /// <summary>
    /// NBP failing part-way through a multi-request backfill never leaves a hole in the stored
    /// rates (which would value dates in it at a stale rate); the worker then fills the rest.
    /// </summary>
    [Fact]
    public async Task PartialBackfillFailure_LeavesNoGap()
    {
        for (var day = new DateOnly(2023, 1, 1); day <= new DateOnly(2024, 12, 31); day = day.AddDays(1))
            _factory.Nbp.Publish(WalletCurrency.EUR, day, 4.5m);
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "EUR savings", currency: "EUR");
        (await PutAsync(h.Owner, wallet.Id, "2024-12-20", "10")).EnsureSuccessStatusCode();

        // The backfill to 2023-01-10 takes two requests; the one covering March 2024 times out.
        var outage = new DateOnly(2024, 3, 1);
        _factory.Nbp.FailWhen = (first, last) => first <= outage && outage <= last;
        (await PutAsync(h.Owner, wallet.Id, "2023-01-10", "10")).EnsureSuccessStatusCode();
        var afterFailure = await StoredEurRatesAsync();

        _factory.Nbp.FailWhen = null;
        await RunWorkerAsync();
        var afterWorker = await StoredEurRatesAsync();

        (afterFailure[^1].DayNumber - afterFailure[0].DayNumber + 1).ShouldBe(afterFailure.Count);
        afterWorker[0].ShouldBe(new DateOnly(2023, 1, 1));
        afterWorker.Count.ShouldBe(731);
    }

    private async Task<List<DateOnly>> StoredEurRatesAsync()
    {
        await using var db = NewDbContext();
        return await db.FxRates
            .Where(r => r.Currency == WalletCurrency.EUR && r.EffectiveDate.Year >= 2023 && r.EffectiveDate.Year <= 2024)
            .OrderBy(r => r.EffectiveDate).Select(r => r.EffectiveDate).ToListAsync(Ct);
    }
}
