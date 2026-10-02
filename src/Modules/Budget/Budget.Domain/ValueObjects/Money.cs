namespace Budget.Domain.ValueObjects;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// A positive amount in the budget's currency (all supported currencies have two decimals), held as
/// exact minor units. Parsed from plain decimal strings only: no sign, exponent, separators or
/// excess precision — never rounded silently.
/// </summary>
public readonly partial record struct Money
{
    /// <summary>Largest single entry: <c>9999999999.99</c>, in minor units.</summary>
    public const long MaxMinorUnits = 999_999_999_999;

    private Money(long minorUnits) => MinorUnits = minorUnits;

    /// <summary>The amount in hundredths of the currency unit.</summary>
    public long MinorUnits { get; }

    /// <summary>The amount as a decimal with two fractional digits.</summary>
    public decimal Amount => MinorUnits / 100m;

    /// <summary>Wraps minor units; the caller guarantees the range.</summary>
    /// <param name="minorUnits">Hundredths of the currency unit, ≥ 0.</param>
    public static Money FromMinorUnits(long minorUnits) => new(minorUnits);

    /// <summary>Wraps a stored decimal.</summary>
    /// <param name="amount">A value with at most two fractional digits.</param>
    public static Money FromDecimal(decimal amount) => new(decimal.ToInt64(amount * 100m));

    /// <summary>Parses <c>123</c>, <c>123.4</c> or <c>123.45</c>; the result is positive and ≤ 9999999999.99.</summary>
    /// <param name="text">Request text.</param>
    /// <param name="money">The parsed amount on success.</param>
    /// <returns>Whether <paramref name="text"/> was a valid positive amount.</returns>
    public static bool TryParsePositive(string? text, out Money money)
        => TryParse(text, allowZero: false, out money);

    /// <summary>Like <see cref="TryParsePositive"/> but also accepts zero (a limit of <c>0.00</c> is meaningful).</summary>
    /// <param name="text">Request text.</param>
    /// <param name="money">The parsed amount on success.</param>
    /// <returns>Whether <paramref name="text"/> was a valid amount ≥ 0.</returns>
    public static bool TryParseNonNegative(string? text, out Money money)
        => TryParse(text, allowZero: true, out money);

    private static bool TryParse(string? text, bool allowZero, out Money money)
    {
        money = default;
        if (text is null || !AmountPattern().IsMatch(text))
            return false;

        var value = decimal.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        var minor = decimal.ToInt64(value * 100m);
        if (minor < (allowZero ? 0 : 1) || minor > MaxMinorUnits)
            return false;

        money = new Money(minor);
        return true;
    }

    /// <summary>The normalised wire form, e.g. <c>"123.40"</c>.</summary>
    public override string ToString() => Amount.ToString("F2", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^\d{1,10}(\.\d{1,2})?$")]
    private static partial Regex AmountPattern();
}
