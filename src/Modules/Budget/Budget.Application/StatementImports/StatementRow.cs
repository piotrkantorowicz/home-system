namespace Budget.Application.StatementImports;

using System.Globalization;

/// <summary>
/// One data row, read by header name. Every accessor throws <see cref="StatementRowException"/> on a
/// missing or malformed cell, so a profile's mapping reads as straight-line code.
/// </summary>
internal sealed class StatementRow(
    int lineNumber,
    string[] fields,
    IReadOnlyDictionary<string, int> columns,
    IReadOnlyDictionary<string, string[]> preamble)
{
    /// <summary>1-based line number in the file.</summary>
    public int LineNumber { get; } = lineNumber;

    /// <summary>
    /// The cell, whitespace collapsed and outer apostrophes removed: mBank's empty <c>''</c> and ING's
    /// <c>'number'</c> wrap a value so spreadsheets keep it as text.
    /// </summary>
    public string Text(string column)
    {
        var value = StatementText.Collapse(fields[Index(column)]);
        return value is ['\'', .. var inner, '\''] ? inner : value;
    }

    /// <summary>The cell right after <paramref name="column"/> (ING follows every amount with its own <c>Waluta</c> column).</summary>
    public string TextAfter(string column)
    {
        var index = Index(column) + 1;
        return index < fields.Length ? StatementText.Collapse(fields[index]) : throw new StatementRowException(StatementIssueCode.MissingColumn);
    }

    /// <summary>All non-empty cells from <paramref name="column"/> to the end of the row (PKO spreads its description over unnamed columns).</summary>
    public IReadOnlyList<string> TextsFrom(string column)
        => [.. fields[Index(column)..].Select(StatementText.Collapse).Where(f => f.Length > 0)];

    /// <summary>A date cell in the exact <paramref name="format"/>.</summary>
    public DateOnly Date(string column, string format)
        => ParseDate(Text(column), format) ?? throw new StatementRowException(StatementIssueCode.InvalidDate);

    /// <summary>A date-and-time cell in the exact <paramref name="format"/>.</summary>
    public DateTime DateTime(string column, string format)
        => System.DateTime.TryParseExact(Text(column), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : throw new StatementRowException(StatementIssueCode.InvalidDate);

    /// <summary>
    /// A signed amount with the given decimal separator; spaces are thousands separators. More than two
    /// decimal places is an issue, never rounded (Budget design §4).
    /// </summary>
    public decimal Amount(string column, char decimalSeparator)
    {
        var text = Text(column).Replace(" ", "", StringComparison.Ordinal);
        if (decimalSeparator != '.')
            text = text.Replace(decimalSeparator, '.');

        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            throw new StatementRowException(StatementIssueCode.InvalidAmount);

        return amount == decimal.Round(amount, 2)
            ? amount
            : throw new StatementRowException(StatementIssueCode.ExcessPrecision);
    }

    /// <summary>A value on the line after a preamble key line (<c>#Key</c> or <c>Key:</c>), or <see langword="null"/>.</summary>
    public string? Preamble(string key, Index index = default)
        => preamble.TryGetValue(key, out var values) && index.GetOffset(values.Length) is var at && at >= 0 && at < values.Length ? values[at] : null;

    /// <summary>Parses an exact-format date, or <see langword="null"/>.</summary>
    public static DateOnly? ParseDate(string text, string format)
        => DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;

    private int Index(string column)
        => columns.TryGetValue(column, out var index) && index < fields.Length
            ? index
            : throw new StatementRowException(StatementIssueCode.MissingColumn);
}
