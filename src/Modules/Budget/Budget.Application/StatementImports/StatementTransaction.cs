namespace Budget.Application.StatementImports;

/// <summary>
/// One statement row, normalised across banks. Nothing here is persisted: the preview derives the
/// candidate, source key and description from it (<c>docs/design/budget/bank-import.md</c> §2.2).
/// </summary>
/// <param name="LineNumber">1-based line number in the file.</param>
/// <param name="AccountId">Bank account the row belongs to, when the file names one; never stored in clear.</param>
/// <param name="BookingDate">Date the bank booked the row; part of the fingerprint.</param>
/// <param name="TransactionDate">Date the purchase happened; becomes the expense's <c>OccurredOn</c>.</param>
/// <param name="TransactionTime">Time of the purchase when the bank exports one (Revolut); part of the fingerprint.</param>
/// <param name="Amount">Signed amount in <paramref name="Currency"/>; negative is money leaving the account, fees included.</param>
/// <param name="Currency">ISO code of the amount.</param>
/// <param name="IsForeignCurrency"><see langword="true"/> when <paramref name="Currency"/> is not the Budget's currency.</param>
/// <param name="Counterparty">Payee or payer as the bank names it; empty when the bank leaves it blank.</param>
/// <param name="Title">Transfer title or card-payment text, whitespace collapsed; empty when none.</param>
/// <param name="BankReference">The bank's own transaction ID, when the export has one.</param>
/// <param name="BankType">The bank's operation type, e.g. <c>ZAKUP PRZY UŻYCIU KARTY</c> or <c>CARD_PAYMENT</c>.</param>
/// <param name="IsBooked"><see langword="false"/> for pending or blocked-funds rows.</param>
internal sealed record StatementTransaction(
    int LineNumber,
    string? AccountId,
    DateOnly BookingDate,
    DateOnly TransactionDate,
    TimeOnly? TransactionTime,
    decimal Amount,
    string Currency,
    bool IsForeignCurrency,
    string Counterparty,
    string Title,
    string? BankReference,
    string BankType,
    bool IsBooked);
