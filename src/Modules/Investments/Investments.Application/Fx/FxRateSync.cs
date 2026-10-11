namespace Investments.Application.Fx;

using Investments.Application.Persistence;
using Investments.Domain.Abstractions;
using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Keeps stored NBP rates covering every foreign-currency balance: back to a few days before the
/// earliest one (so a weekend or holiday finds the prior business day) and forward to today.
/// Currencies with no balances cost no NBP call.
/// </summary>
internal sealed class FxRateSync(IInvestmentsReadDbContext db, IFxRateStore store, INbpClient nbp)
{
    /// <summary>NBP's longest range per request.</summary>
    private const int MaxDaysPerRequest = 367;

    /// <summary>Longer than any run of Polish non-business days.</summary>
    private const int LookbackDays = 10;

    /// <summary>The currencies NBP is asked about.</summary>
    public static readonly WalletCurrency[] ForeignCurrencies = [WalletCurrency.EUR, WalletCurrency.USD];

    /// <summary>Syncs every foreign currency; the worker's run.</summary>
    /// <param name="today">The household's current date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="NbpUnavailableException">NBP failed; rates fetched before the failure are kept.</exception>
    public async Task SyncAllAsync(DateOnly today, CancellationToken ct)
    {
        foreach (var currency in ForeignCurrencies)
            await SyncAsync(currency, today, ct);
    }

    /// <summary>Fetches the rates missing before the earliest balance and after the latest stored rate.</summary>
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
        var from = earliestBalance.Value.AddDays(-LookbackDays);

        if (earliest is null || latest is null)
        {
            await FetchAsync(currency, from, today, ct);
            return;
        }

        if (earliestBalance < earliest)
            await FetchAsync(currency, from, earliest.Value.AddDays(-1), ct);
        if (latest < today)
            await FetchAsync(currency, latest.Value.AddDays(1), today, ct);
    }

    private async Task FetchAsync(WalletCurrency currency, DateOnly from, DateOnly to, CancellationToken ct)
    {
        for (var start = from; start <= to; start = start.AddDays(MaxDaysPerRequest))
        {
            var end = start.AddDays(MaxDaysPerRequest - 1);
            var rates = await nbp.GetRatesAsync(currency, start, end < to ? end : to, ct);
            if (rates.Count > 0)
                await store.AddMissingAsync([.. rates.Select(r => FxRate.Create(currency, r.EffectiveDate, r.Mid, r.TableNo))], ct);
        }
    }
}
