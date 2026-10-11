namespace Investments.Domain.Fx;

using Investments.Domain.ValueObjects;

/// <summary>
/// Converts amounts to PLN with stored NBP rates. The rate for a date is the most recent one
/// published on or before it, so weekends and holidays use the prior business day. No rate on or
/// before the date means the value is missing — a later rate is never used.
/// </summary>
public sealed class FxRateBook
{
    private readonly Dictionary<WalletCurrency, FxQuote[]> _byCurrency;

    /// <summary>Creates a book over the given rates.</summary>
    /// <param name="rates">Rates for any currencies and dates, in any order.</param>
    public FxRateBook(IEnumerable<FxQuote> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        _byCurrency = rates
            .GroupBy(r => r.Currency)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.EffectiveDate).ToArray());
    }

    /// <summary>The rate that applies to <paramref name="date"/>, or <see langword="null"/> when none was published on or before it.</summary>
    /// <param name="currency">A foreign currency.</param>
    /// <param name="date">The date being valued.</param>
    public FxQuote? Find(WalletCurrency currency, DateOnly date)
        => _byCurrency.TryGetValue(currency, out var rates) ? rates.LastOrDefault(r => r.EffectiveDate <= date) : null;

    /// <summary>Converts an amount to PLN at the newest rate in the book — today's rate, as rates are never future-dated.</summary>
    /// <param name="amount">The amount in its own currency.</param>
    /// <param name="currency">Its currency.</param>
    public PlnValue ToPlnAtLatest(decimal amount, WalletCurrency currency) => ToPln(amount, currency, DateOnly.MaxValue);

    /// <summary>Converts an amount held in <paramref name="currency"/> on <paramref name="date"/> to PLN.</summary>
    /// <param name="amount">The amount in its own currency.</param>
    /// <param name="currency">Its currency.</param>
    /// <param name="date">The date it is valued at.</param>
    public PlnValue ToPln(decimal amount, WalletCurrency currency, DateOnly date)
    {
        if (currency == WalletCurrency.PLN)
            return new PlnValue(amount, null, RateMissing: false);

        var rate = Find(currency, date);
        return rate is null
            ? new PlnValue(null, null, RateMissing: true)
            : new PlnValue(Math.Round(amount * rate.Mid, 2, MidpointRounding.AwayFromZero), rate, RateMissing: false);
    }
}
