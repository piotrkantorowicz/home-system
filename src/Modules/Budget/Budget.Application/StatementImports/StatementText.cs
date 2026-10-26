namespace Budget.Application.StatementImports;

using System.Text.RegularExpressions;
using Budget.Domain.Aggregates;

/// <summary>Text clean-up shared by the bank profiles and the duplicate fingerprint.</summary>
internal static partial class StatementText
{
    /// <summary>Trims and collapses every whitespace run, including no-break spaces, to one space.</summary>
    public static string Collapse(string value) => Whitespace().Replace(value, " ").Trim();

    /// <summary>
    /// The description form compared by the fingerprint (<c>bank-import.md</c> §4.1): collapsed,
    /// upper-case invariant, card-number masks such as <c>Nr karty 4246xx******1234</c> removed.
    /// </summary>
    public static string NormalizeForMatching(string value)
        => Collapse(CardMask().Replace(Collapse(value).ToUpperInvariant(), " "));

    /// <summary>
    /// The expense description proposed for a row (§5): <c>counterparty — title</c>, or whichever is
    /// present, cut to the expense description limit.
    /// </summary>
    public static string ExpenseDescription(StatementTransaction transaction)
    {
        string[] parts = [.. new[] { transaction.Counterparty, transaction.Title }.Where(p => p.Length > 0)];
        var description = string.Join(" — ", parts);
        return description.Length <= Expense.MaxDescriptionLength ? description : description[..Expense.MaxDescriptionLength].TrimEnd();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"(?:NR\s+KARTY:?\s*)?[0-9X]*\*{2,}[0-9X*]*")]
    private static partial Regex CardMask();
}
