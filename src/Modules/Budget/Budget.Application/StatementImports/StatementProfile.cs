namespace Budget.Application.StatementImports;

/// <summary>How to recognise and read one bank's CSV export (<c>bank-import.md</c> §2.1).</summary>
/// <param name="Name">Stable profile name, e.g. <c>mbank</c>.</param>
/// <param name="Delimiter">Field delimiter.</param>
/// <param name="Utf8Only">The bank exports UTF-8 only; a file that is not valid UTF-8 is rejected.</param>
/// <param name="Header">The leading header cells that identify the export, compared exactly.</param>
/// <param name="Map">Turns a data row into a transaction; the currency is the Budget's.</param>
/// <param name="Period">Reads the statement period from the preamble, when the bank states one.</param>
internal sealed record StatementProfile(
    string Name,
    char Delimiter,
    bool Utf8Only,
    IReadOnlyList<string> Header,
    Func<StatementRow, string, StatementTransaction> Map,
    Func<IReadOnlyDictionary<string, string[]>, (DateOnly? From, DateOnly? To)>? Period = null);
