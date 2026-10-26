namespace Budget.Application.StatementImports;

using System.Text;
using Budget.Domain.Exceptions;
using Budget.Domain.ValueObjects;

/// <summary>
/// Reads a bank statement export into transactions, in memory and without persistence
/// (<c>docs/design/budget/bank-import.md</c> §2). The bank is detected from the header row; a
/// malformed data row becomes an issue with its line number and never stops the rest of the file.
/// </summary>
internal static class StatementParser
{
    /// <summary>Largest accepted file (§8).</summary>
    public const int MaxFileBytes = 2 * 1024 * 1024;

    /// <summary>Most data rows in one file (§8).</summary>
    public const int MaxDataRows = 1000;

    /// <summary>How far into the file the header row may sit; the mBank preamble alone is ~37 lines (§8).</summary>
    public const int HeaderSearchLines = 60;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Parses <paramref name="content"/>; rows in another currency than <paramref name="budgetCurrency"/> are flagged.</summary>
    /// <exception cref="BudgetDomainException">Too large, too many rows, not a supported bank export, or not valid UTF-8 for a UTF-8-only bank.</exception>
    public static StatementParseResult Parse(byte[] content, BudgetCurrency budgetCurrency)
    {
        if (content.Length > MaxFileBytes)
            throw new BudgetDomainException($"A statement file can be at most {MaxFileBytes / 1024 / 1024} MiB.");

        var (text, isUtf8) = Decode(content);
        string[] lines = text.TrimStart('\uFEFF').Split('\n');
        var (profile, headerLine) = Detect(lines);
        if (profile.Utf8Only && !isUtf8)
            throw new BudgetDomainException($"The {profile.Name} statement is not valid UTF-8. Export it again without converting it.");

        var preamble = ReadPreamble(lines.AsSpan(0, headerLine), profile.Delimiter);
        var columns = ReadColumns(CsvLine.Split(lines[headerLine].TrimEnd('\r'), profile.Delimiter));
        var currency = budgetCurrency.ToString();
        List<StatementTransaction> transactions = [];
        List<StatementIssue> issues = [];

        for (var i = headerLine + 1; i < lines.Length; i++)
        {
            string[] fields = CsvLine.Split(lines[i].TrimEnd('\r'), profile.Delimiter);
            var first = fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f))?.Trim();
            // ponytail: data ends at the first blank or '#' line (mBank's closing balance and legal note);
            // a stray blank line mid-file would end it early, which no supported bank exports.
            if (first is null || first.StartsWith('#'))
                break;
            if (transactions.Count + issues.Count == MaxDataRows)
                throw new BudgetDomainException($"A statement file can have at most {MaxDataRows} transactions. Export a shorter date range.");

            var row = new StatementRow(i + 1, fields, columns, preamble);
            try
            {
                transactions.Add(profile.Map(row, currency));
            }
            catch (StatementRowException ex)
            {
                issues.Add(new StatementIssue(row.LineNumber, ex.Code));
            }
        }

        var (from, to) = profile.Period?.Invoke(preamble) ?? (null, null);
        return new StatementParseResult(profile.Name, from, to, transactions, issues);
    }

    /// <summary>Strict UTF-8 first; on invalid bytes, Windows-1250 — the Polish banks' export encoding (§2.1).</summary>
    private static (string Text, bool IsUtf8) Decode(byte[] content)
    {
        try
        {
            return (StrictUtf8.GetString(content), true);
        }
        catch (DecoderFallbackException)
        {
            // Not null: the code-pages provider ships with .NET and always has 1250.
            return (CodePagesEncodingProvider.Instance.GetEncoding(1250)!.GetString(content), false);
        }
    }

    private static (StatementProfile Profile, int HeaderLine) Detect(string[] lines)
    {
        List<(StatementProfile, int)> matches = [];
        for (var i = 0; i < Math.Min(lines.Length, HeaderSearchLines); i++)
        {
            foreach (var profile in StatementProfiles.All)
            {
                string[] cells = CsvLine.Split(lines[i].TrimEnd('\r'), profile.Delimiter);
                if (cells.Length >= profile.Header.Count && profile.Header.Select((h, c) => cells[c].Trim() == h).All(m => m))
                    matches.Add((profile, i));
            }
        }

        return matches.Count == 1
            ? matches[0]
            : throw new BudgetDomainException("Unrecognised statement format. Export the account history as CSV from PKO BP, mBank, ING or Revolut.");
    }

    /// <summary>
    /// <c>#Key</c> and <c>Key:</c> lines and the values on the line after each (mBank's account, currency and
    /// period; ING's <c>Wybrane rachunki:</c>).
    /// </summary>
    private static Dictionary<string, string[]> ReadPreamble(ReadOnlySpan<string> lines, char delimiter)
    {
        Dictionary<string, string[]> preamble = [];
        for (var i = 0; i + 1 < lines.Length; i++)
        {
            string[] key = Cells(lines[i], delimiter);
            if (key.Length > 0 && (key[0].StartsWith('#') || key[0].EndsWith(':')))
                preamble.TryAdd(key[0], Cells(lines[i + 1], delimiter));
        }

        return preamble;
    }

    private static string[] Cells(string line, char delimiter)
        => [.. CsvLine.Split(line.TrimEnd('\r'), delimiter).Select(StatementText.Collapse).Where(c => c.Length > 0)];

    /// <summary>Header name to index; a repeated name keeps its first position (read ING's repeated <c>Waluta</c> with <see cref="StatementRow.TextAfter"/>).</summary>
    private static Dictionary<string, int> ReadColumns(string[] header)
    {
        Dictionary<string, int> columns = [];
        for (var i = 0; i < header.Length; i++)
            columns.TryAdd(header[i].Trim(), i);

        return columns;
    }
}
