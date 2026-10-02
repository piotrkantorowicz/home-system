namespace Budget.UnitTests.Domain;

using global::Budget.Domain.ValueObjects;

/// <summary>Month parsing and boundaries.</summary>
public sealed class BudgetMonthTests
{
    /// <summary>Valid months parse, normalise and expose a half-open range.</summary>
    [Theory]
    [InlineData("2026-10", "2026-10-01", "2026-11-01")]
    [InlineData("2026-12", "2026-12-01", "2027-01-01")]
    [InlineData("2028-02", "2028-02-01", "2028-03-01")]
    [InlineData("2026-02", "2026-02-01", "2026-03-01")]
    public void TryParse_Valid_GivesHalfOpenRange(string text, string start, string next)
    {
        BudgetMonth.TryParse(text, out var month).ShouldBeTrue();

        month.Start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).ShouldBe(start);
        month.NextStart.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).ShouldBe(next);
        month.ToString().ShouldBe(text);
    }

    /// <summary>Malformed, out-of-range and non-month text is rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("2026-1")]
    [InlineData("2026-10-01")]
    [InlineData("26-10")]
    [InlineData("1999-12")]
    [InlineData("abcd-ef")]
    [InlineData(null)]
    public void TryParse_Invalid(string? text)
        => BudgetMonth.TryParse(text, out _).ShouldBeFalse();
}
