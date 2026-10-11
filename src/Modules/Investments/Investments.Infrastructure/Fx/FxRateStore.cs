namespace Investments.Infrastructure.Fx;

using Investments.Domain.Abstractions;
using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;
using Investments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class FxRateStore(InvestmentsDbContext dbContext) : IFxRateStore
{
    public async Task<(DateOnly? Earliest, DateOnly? Latest)> GetStoredRangeAsync(WalletCurrency currency, CancellationToken ct)
    {
        var range = await dbContext.FxRates.AsNoTracking()
            .Where(r => r.Currency == currency)
            .GroupBy(r => r.Currency)
            .Select(g => new { Earliest = g.Min(r => r.EffectiveDate), Latest = g.Max(r => r.EffectiveDate) })
            .FirstOrDefaultAsync(ct);

        return (range?.Earliest, range?.Latest);
    }

    public async Task AddMissingAsync(IReadOnlyCollection<FxRate> rates, CancellationToken ct)
    {
        string[] currencies = [.. rates.Select(r => r.Currency.ToString())];
        DateOnly[] dates = [.. rates.Select(r => r.EffectiveDate)];
        decimal[] mids = [.. rates.Select(r => r.Mid)];
        string[] tables = [.. rates.Select(r => r.TableNo)];

        // One statement; rows already stored (by an earlier run or a racing one) are skipped.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO fx_rates (currency, effective_date, mid, table_no)
            SELECT * FROM unnest({currencies}, {dates}, {mids}, {tables})
            ON CONFLICT (currency, effective_date) DO NOTHING
            """,
            ct);
    }
}
