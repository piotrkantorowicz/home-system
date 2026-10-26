namespace Budget.Application.StatementImports;

/// <summary>A data row that could not be parsed, by its 1-based line number in the file.</summary>
internal sealed record StatementIssue(int LineNumber, StatementIssueCode Code);
