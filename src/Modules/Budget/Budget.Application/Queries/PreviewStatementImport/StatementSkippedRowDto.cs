namespace Budget.Application.Queries.PreviewStatementImport;

/// <summary>A parsed row that will not become an expense, shown so nothing is dropped silently.</summary>
/// <param name="LineNumber">1-based line in the file.</param>
/// <param name="OccurredOn">Transaction date.</param>
/// <param name="Amount">Signed decimal string; positive is money in.</param>
/// <param name="Currency">The row's own currency.</param>
/// <param name="Description">Bank description, at most 80 characters.</param>
/// <param name="Reason"><c>NotBooked</c>, <c>Incoming</c>, <c>CurrencyMismatch</c>, <c>OwnTransfer</c> or <c>CashWithdrawal</c>.</param>
public sealed record StatementSkippedRowDto(int LineNumber, DateOnly OccurredOn, string Amount, string Currency, string Description, string Reason);
