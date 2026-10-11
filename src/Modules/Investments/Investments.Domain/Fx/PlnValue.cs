namespace Investments.Domain.Fx;

/// <summary>An amount converted to PLN, with the rate used.</summary>
/// <param name="Amount">The PLN amount, two decimals; <see langword="null"/> when <see cref="RateMissing"/>.</param>
/// <param name="Rate">The rate used; <see langword="null"/> for PLN amounts and when missing.</param>
/// <param name="RateMissing">A foreign amount with no rate on or before its date.</param>
public sealed record PlnValue(decimal? Amount, FxRate? Rate, bool RateMissing);
