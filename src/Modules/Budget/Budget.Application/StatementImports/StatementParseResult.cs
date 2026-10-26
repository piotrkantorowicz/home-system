namespace Budget.Application.StatementImports;

/// <summary>Everything read from one statement file: parsed rows and the rows that could not be parsed.</summary>
/// <param name="Profile">The detected bank profile, e.g. <c>mbank</c>.</param>
/// <param name="PeriodFrom">First day the statement covers, when the file states it.</param>
/// <param name="PeriodTo">Last day the statement covers, when the file states it.</param>
/// <param name="Transactions">Parsed data rows in file order.</param>
/// <param name="Issues">Data rows that could not be parsed, in file order.</param>
internal sealed record StatementParseResult(
    string Profile,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo,
    IReadOnlyList<StatementTransaction> Transactions,
    IReadOnlyList<StatementIssue> Issues);
