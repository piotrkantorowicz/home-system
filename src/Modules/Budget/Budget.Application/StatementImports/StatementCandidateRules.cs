namespace Budget.Application.StatementImports;

/// <summary>Which parsed rows become expense candidates (<c>bank-import.md</c> §3).</summary>
internal static class StatementCandidateRules
{
    // ponytail: matched by substring of the bank's upper-cased operation type. Only the mBank card
    // repayment is seen in a real export; extend these lists as real PKO, ING and Revolut exports arrive.
    private static readonly string[] OwnTransferTypes = ["SPŁATA KARTY", "PRZELEW WŁASNY", "RACHUNEK WŁASNY", "EXCHANGE", "TOPUP"];
    private static readonly string[] CashWithdrawalTypes = ["BANKOMA", "WYPŁATA GOTÓWKI", "ATM"]; // BANKOMA: BANKOMAT, W BANKOMACIE

    /// <summary>Why the row is skipped, or <see langword="null"/> when it is a candidate.</summary>
    public static StatementSkipReason? SkipReason(StatementTransaction transaction)
    {
        var type = transaction.BankType.ToUpperInvariant();
        return transaction switch
        {
            { IsBooked: false } => StatementSkipReason.NotBooked,
            { Amount: >= 0 } => StatementSkipReason.Incoming,
            { IsForeignCurrency: true } => StatementSkipReason.CurrencyMismatch,
            _ when OwnTransferTypes.Any(t => type.Contains(t, StringComparison.Ordinal)) => StatementSkipReason.OwnTransfer,
            _ when CashWithdrawalTypes.Any(t => type.Contains(t, StringComparison.Ordinal)) => StatementSkipReason.CashWithdrawal,
            _ => null,
        };
    }
}
