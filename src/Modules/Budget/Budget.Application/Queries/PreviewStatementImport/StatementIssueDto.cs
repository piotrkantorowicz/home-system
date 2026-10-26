namespace Budget.Application.Queries.PreviewStatementImport;

/// <summary>A row that could not be parsed. Carries no row contents.</summary>
/// <param name="LineNumber">1-based line in the file.</param>
/// <param name="Code"><c>MissingColumn</c>, <c>InvalidDate</c>, <c>InvalidAmount</c> or <c>ExcessPrecision</c>.</param>
public sealed record StatementIssueDto(int LineNumber, string Code);
