namespace Budget.Application.Queries.PreviewStatementImport;

/// <summary>What importing a statement into one envelope would do. Nothing has been written.</summary>
/// <param name="Profile">The detected bank export, e.g. <c>mbank</c>.</param>
/// <param name="PeriodFrom">First day the statement covers, when the file states it.</param>
/// <param name="PeriodTo">Last day the statement covers, when the file states it.</param>
/// <param name="AccountId">The envelope previewed for.</param>
/// <param name="Candidates">Outgoing booked rows that can become expenses, in file order.</param>
/// <param name="Skipped">Parsed rows that will not become expenses, with the reason.</param>
/// <param name="Issues">Rows that could not be parsed.</param>
public sealed record StatementPreviewDto(
    string Profile,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo,
    Guid AccountId,
    IReadOnlyList<StatementCandidateDto> Candidates,
    IReadOnlyList<StatementSkippedRowDto> Skipped,
    IReadOnlyList<StatementIssueDto> Issues);
