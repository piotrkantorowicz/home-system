namespace Budget.Application.Queries.PreviewStatementImport;

using Budget.Application.Queries.GetExpense;

/// <summary>A statement row that can become an expense (<c>bank-import.md</c> §4.3).</summary>
/// <param name="LineNumber">1-based line in the file.</param>
/// <param name="SourceKey">64 lowercase hex characters; send back on import.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="Amount">Positive decimal string, fees included.</param>
/// <param name="Description">Proposed description, at most 80 characters.</param>
/// <param name="SuggestedCategory">Category learned from earlier imports; <see langword="null"/> until #673.</param>
/// <param name="Status"><c>New</c>, <c>AlreadyImported</c> (not selectable) or <c>PossibleDuplicate</c>.</param>
/// <param name="Matches">For <c>PossibleDuplicate</c>: up to five visible expenses with the same amount within two days.</param>
public sealed record StatementCandidateDto(
    int LineNumber,
    string SourceKey,
    DateOnly OccurredOn,
    string Amount,
    string Description,
    string? SuggestedCategory,
    string Status,
    IReadOnlyList<ExpenseDto> Matches);
