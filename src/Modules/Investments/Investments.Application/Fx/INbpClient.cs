namespace Investments.Application.Fx;

using Investments.Domain.ValueObjects;

/// <summary>Reads NBP table A mid rates. One implementation; the seam exists so tests can fake NBP.</summary>
public interface INbpClient
{
    /// <summary>
    /// Every published rate for <paramref name="currency"/> in <c>[first, last]</c> (at most 367 days);
    /// empty when NBP published none in the range.
    /// </summary>
    /// <param name="currency">A foreign currency.</param>
    /// <param name="first">First date, inclusive.</param>
    /// <param name="last">Last date, inclusive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="NbpUnavailableException">NBP could not be reached or answered with an error.</exception>
    Task<IReadOnlyList<NbpRate>> GetRatesAsync(WalletCurrency currency, DateOnly first, DateOnly last, CancellationToken ct);
}
