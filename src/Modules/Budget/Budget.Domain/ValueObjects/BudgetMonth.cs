namespace Budget.Domain.ValueObjects;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>A calendar month, written <c>YYYY-MM</c>. Spending falls in the month of its purchase date.</summary>
public readonly partial record struct BudgetMonth
{
    private BudgetMonth(DateOnly start) => Start = start;

    /// <summary>The first day of the month.</summary>
    public DateOnly Start { get; }

    /// <summary>The first day of the next month — the exclusive end of the month's range.</summary>
    public DateOnly NextStart => Start.AddMonths(1);

    /// <summary>The month containing <paramref name="start"/>'s year and month.</summary>
    /// <param name="start">Any day; only its year and month are kept.</param>
    public static BudgetMonth Of(DateOnly start) => new(new DateOnly(start.Year, start.Month, 1));

    /// <summary>Parses <c>YYYY-MM</c> (years 2000–2999, months 01–12).</summary>
    /// <param name="text">Request text.</param>
    /// <param name="month">The parsed month on success.</param>
    /// <returns>Whether <paramref name="text"/> was a valid month.</returns>
    public static bool TryParse(string? text, out BudgetMonth month)
    {
        month = default;
        if (text is null || !Pattern().IsMatch(text))
            return false;

        var year = int.Parse(text.AsSpan(0, 4), CultureInfo.InvariantCulture);
        var number = int.Parse(text.AsSpan(5, 2), CultureInfo.InvariantCulture);
        if (number is < 1 or > 12)
            return false;

        month = new BudgetMonth(new DateOnly(year, number, 1));
        return true;
    }

    /// <summary>The wire form, e.g. <c>2026-02</c>.</summary>
    public override string ToString() => Start.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^2\d{3}-\d{2}$")]
    private static partial Regex Pattern();
}
