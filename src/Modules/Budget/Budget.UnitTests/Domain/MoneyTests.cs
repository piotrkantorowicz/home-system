namespace Budget.UnitTests.Domain;

using global::Budget.Domain.ValueObjects;

/// <summary>Strict money parsing: positive, two decimals, no exponent/sign/separators, capped.</summary>
public sealed class MoneyTests
{
    /// <summary>Plain decimal strings parse to exact minor units and normalise to two decimals.</summary>
    [Theory]
    [InlineData("1", 100, "1.00")]
    [InlineData("12.3", 1230, "12.30")]
    [InlineData("123.45", 12345, "123.45")]
    [InlineData("0.01", 1, "0.01")]
    [InlineData("9999999999.99", 999_999_999_999, "9999999999.99")]
    public void TryParsePositive_Valid(string text, long minor, string normalised)
    {
        Money.TryParsePositive(text, out var money).ShouldBeTrue();

        money.MinorUnits.ShouldBe(minor);
        money.ToString().ShouldBe(normalised);
    }

    /// <summary>Zero, negatives, excess precision, exponent, separators, blanks and over-cap values are rejected, never rounded.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("0.00")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.234")]
    [InlineData("1e3")]
    [InlineData("1,50")]
    [InlineData("1 000")]
    [InlineData(".5")]
    [InlineData("1.")]
    [InlineData("")]
    [InlineData(" 1")]
    [InlineData("10000000000.00")]
    [InlineData("12345678901")]
    [InlineData(null)]
    public void TryParsePositive_Invalid(string? text)
        => Money.TryParsePositive(text, out _).ShouldBeFalse();
}
