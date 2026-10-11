namespace Investments.Application.Fx;

using Investments.Application.Persistence;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Keeps stored NBP rates covering every foreign-currency balance: back to a few days before the
/// earliest one (so a weekend or holiday finds the prior business day) and forward to today.
/// Currencies with no balances cost no NBP call.
/// </summary>
internal sealed partial class FxRateSync(
    IInvestmentsReadDbContext db, IFxRateStore store, INbpClient nbp, ILogger<FxRateSync> logger)
{
    /// <summary>NBP's longest range per request.</summary>
    private const int MaxDaysPerRequest = 367;

    /// <summary>Longer than any run of Polish non-business days.</summary>
    private const int LookbackDays = 10;

    /// <summary>The currencies NBP is asked about.</summary>
    public static readonly WalletCurrency[] ForeignCurrencies = [WalletCurrency.EUR, WalletCurrency.USD];

    /// <summary>Syncs every foreign currency; the worker's run. A failing currency never blocks the others.</summary>
    /// <param name="today">The household's current date.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SyncAllAsync(DateOnly today, CancellationToken ct)
    {
        foreach (var currency in ForeignCurrencies)
            await TrySyncAsync(currency, today, ct);
    }

    /// <summary>
    /// <see cref="SyncAsync"/> that logs a failure instead of throwing, for callers that already
    /// committed a write: the read side shows <c>rateMissing</c> until the worker fills the gap.
    /// </summary>
    /// <param name="currency">A foreign currency.</param>
    /// <param name="today">The household's current date.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task TrySyncAsync(WalletCurrency currency, DateOnly today, CancellationToken ct)
    {
        try
        {
            await SyncAsync(currency, today, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSyncFailed(ex, currency);
        }
    }

    /// <summary>
    /// Fetches the rates after the latest stored one, then those missing before the earliest balance.
    /// Both directions grow the stored range from its edge outwards, so a failure part-way never
    /// leaves a gap: the next run continues from the new edge.
    /// </summary>
    /// <param name="currency">A foreign currency.</param>
    /// <param name="today">The household's current date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="NbpUnavailableException">NBP failed; rates fetched before the failure are kept.</exception>
    public async Task SyncAsync(WalletCurrency currency, DateOnly today, CancellationToken ct)
    {
        var earliestBalance = await db.BalanceSnapshots
            .Where(s => db.Wallets.Any(w => w.Id == s.WalletId && w.Currency == currency))
            .MinAsync(s => (DateOnly?)s.AsOf, ct);
        if (earliestBalance is null)
            return;

        var (earliest, latest) = await store.GetStoredRangeAsync(currency, ct);
        var lookback = earliestBalance.Value.AddDays(-LookbackDays);
        var from = lookback < BalanceSnapshot.EarliestAsOf ? BalanceSnapshot.EarliestAsOf : lookback;

        if (earliest is null || latest is null)
        {
            await FetchBackwardAsync(currency, from, today, ct);
            return;
        }

        if (latest < today)
            await FetchForwardAsync(currency, latest.Value.AddDays(1), today, ct);
        if (earliestBalance < earliest)
            await FetchBackwardAsync(currency, from, earliest.Value.AddDays(-1), ct);
    }

    private async Task FetchForwardAsync(WalletCurrency currency, DateOnly from, DateOnly to, CancellationToken ct)
    {
        for (var start = from; start <= to; start = start.AddDays(MaxDaysPerRequest))
        {
            var end = start.AddDays(MaxDaysPerRequest - 1);
            await FetchAsync(currency, start, end < to ? end : to, ct);
        }
    }

    private async Task FetchBackwardAsync(WalletCurrency currency, DateOnly from, DateOnly to, CancellationToken ct)
    {
        for (var end = to; end >= from; end = end.AddDays(-MaxDaysPerRequest))
        {
            var start = end.AddDays(-(MaxDaysPerRequest - 1));
            await FetchAsync(currency, start > from ? start : from, end, ct);
        }
    }

    private async Task FetchAsync(WalletCurrency currency, DateOnly first, DateOnly last, CancellationToken ct)
    {
        var rates = await nbp.GetRatesAsync(currency, first, last, ct);
        if (rates.Count > 0)
            await store.AddMissingAsync([.. rates.Select(r => FxRate.Create(currency, r.EffectiveDate, r.Mid, r.TableNo))], ct);
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "NBP rate sync for {Currency} failed; the worker will retry")]
    private partial void LogSyncFailed(Exception exception, WalletCurrency currency);
}
