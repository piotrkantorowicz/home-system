namespace Investments.Domain.Abstractions;

using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;

/// <summary>Write side of the stored NBP rates. Reads go through the read context.</summary>
public interface IFxRateStore
{
    /// <summary>The earliest and latest stored effective dates for a currency; both <see langword="null"/> when none.</summary>
    /// <param name="currency">A foreign currency.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<(DateOnly? Earliest, DateOnly? Latest)> GetStoredRangeAsync(WalletCurrency currency, CancellationToken ct);

    /// <summary>Inserts the rates that are not stored yet and ignores the rest — safe to repeat and to race.</summary>
    /// <param name="rates">Rates read from NBP.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddMissingAsync(IReadOnlyCollection<FxRate> rates, CancellationToken ct);
}
