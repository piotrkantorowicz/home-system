namespace Investments.Domain.ValueObjects;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// A non-negative amount in a wallet's currency (all supported currencies have two decimals), held
/// as exact minor units. Parsed from plain decimal strings only: no sign, exponent, separators or
/// excess precision — never rounded silently.
/// </summary>
public readonly partial record struct Money
{
    /// <summary>Largest balance: <c>9999999999.99</c>, in minor units.</summary>
    public const long MaxMinorUnits = 999_999_999_999;

    private Money(long minorUnits) => MinorUnits = minorUnits;

    /// <summary>The amount in hundredths of the currency unit.</summary>
    public long MinorUnits { get; }

    /// <summary>The amount as a decimal with two fractional digits.</summary>
    public decimal Amount => MinorUnits / 100m;

    /// <summary>Parses <c>0</c>, <c>123.4</c> or <c>123.45</c>; the result is ≥ 0 and ≤ 9999999999.99.</summary>
    /// <param name="text">Request text.</param>
    /// <param name="money">The parsed amount on success.</param>
    /// <returns>Whether <paramref name="text"/> was a valid amount.</returns>
    public static bool TryParse(string? text, out Money money)
    {
        money = default;
        if (text is null || !AmountPattern().IsMatch(text))
            return false;

        var minor = decimal.ToInt64(decimal.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) * 100m);
        if (minor > MaxMinorUnits)
            return false;

        money = new Money(minor);
        return true;
    }

    /// <summary>The normalised wire form, e.g. <c>"123.40"</c>.</summary>
    public override string ToString() => Amount.ToString("F2", CultureInfo.InvariantCulture);

    // [0-9], not \d: \d matches any Unicode digit, which decimal.Parse then rejects.
    [GeneratedRegex(@"^[0-9]{1,10}(\.[0-9]{1,2})?$")]
    private static partial Regex AmountPattern();
}
