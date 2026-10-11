namespace Investments.UnitTests.Domain;

using Investments.Domain.ValueObjects;

/// <summary>Strict money parsing: not negative, two decimals, no exponent/sign/separators, capped.</summary>
public sealed class MoneyTests
{
    /// <summary>Plain decimal strings, zero included, parse to exact minor units.</summary>
    /// <param name="text">Input.</param>
    /// <param name="minor">Expected minor units.</param>
    /// <param name="normalised">Expected wire form.</param>
    [Theory]
    [InlineData("0", 0, "0.00")]
    [InlineData("12.3", 1230, "12.30")]
    [InlineData("12450.10", 1_245_010, "12450.10")]
    [InlineData("9999999999.99", 999_999_999_999, "9999999999.99")]
    public void TryParse_Valid(string text, long minor, string normalised)
    {
        Money.TryParse(text, out var money).ShouldBeTrue();

        money.MinorUnits.ShouldBe(minor);
        money.ToString().ShouldBe(normalised);
    }

    /// <summary>Negatives, excess precision, exponent, separators, blanks and over-cap values are rejected, never rounded.</summary>
    /// <param name="text">Input.</param>
    [Theory]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.234")]
    [InlineData("1e3")]
    [InlineData("1,50")]
    [InlineData(".5")]
    [InlineData("")]
    [InlineData("10000000000")]
    [InlineData(null)]
    public void TryParse_Invalid(string? text) => Money.TryParse(text, out _).ShouldBeFalse();
}
