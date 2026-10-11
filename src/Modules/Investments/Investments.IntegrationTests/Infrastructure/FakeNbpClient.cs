namespace Investments.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using global::Investments.Application.Fx;
using global::Investments.Domain.ValueObjects;

/// <summary>Stands in for NBP: serves the rates a test publishes, or fails on demand.</summary>
public sealed class FakeNbpClient : INbpClient
{
    private readonly ConcurrentDictionary<(WalletCurrency, DateOnly), NbpRate> _rates = new();

    /// <summary>When set, every call fails as if NBP were down.</summary>
    public bool Unavailable { get; set; }

    /// <summary>Makes a rate available.</summary>
    /// <param name="currency">The currency.</param>
    /// <param name="date">Effective date.</param>
    /// <param name="mid">PLN per unit.</param>
    public void Publish(WalletCurrency currency, DateOnly date, decimal mid)
        => _rates[(currency, date)] = new NbpRate(date, mid, $"{date.DayOfYear:000}/A/NBP/{date.Year}");

    /// <inheritdoc />
    public Task<IReadOnlyList<NbpRate>> GetRatesAsync(WalletCurrency currency, DateOnly first, DateOnly last, CancellationToken ct)
    {
        if (Unavailable)
            throw new NbpUnavailableException("NBP is down (fake).");
        if (last.DayNumber - first.DayNumber >= 367)
            throw new InvalidOperationException("NBP serves at most 367 days per request.");

        IReadOnlyList<NbpRate> rates = [.. _rates
            .Where(r => r.Key.Item1 == currency && r.Key.Item2 >= first && r.Key.Item2 <= last)
            .Select(r => r.Value)
            .OrderBy(r => r.EffectiveDate)];
        return Task.FromResult(rates);
    }
}
