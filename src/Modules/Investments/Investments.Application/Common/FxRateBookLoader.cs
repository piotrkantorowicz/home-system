namespace Investments.Application.Common;

using Investments.Application.Persistence;
using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal static class FxRateBookLoader
{
    // ponytail: a rate older than this before the earliest date counts as missing; NBP never skips
    // a month, so only a long worker outage would show it. Widen if that ever matters.
    private const int LookbackDays = 31;

    /// <summary>Loads the stored rates needed to value the given (currency, date) pairs.</summary>
    public static async Task<FxRateBook> LoadRatesAsync(
        this IInvestmentsReadDbContext db, IEnumerable<(WalletCurrency Currency, DateOnly Date)> values, CancellationToken ct)
    {
        var foreign = values.Where(v => v.Currency != WalletCurrency.PLN).ToList();
        if (foreign.Count == 0)
            return new FxRateBook([]);

        var currencies = foreign.Select(v => v.Currency).Distinct().ToList();
        var from = foreign.Min(v => v.Date).AddDays(-LookbackDays);
        var to = foreign.Max(v => v.Date);

        var rates = await db.FxRates.AsNoTracking()
            .Where(r => currencies.Contains(r.Currency) && r.EffectiveDate >= from && r.EffectiveDate <= to)
            .ToListAsync(ct);

        return new FxRateBook(rates);
    }
}
