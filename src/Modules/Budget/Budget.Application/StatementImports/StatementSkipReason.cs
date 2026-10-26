namespace Budget.Application.StatementImports;

/// <summary>Why a parsed statement row is not an expense candidate (<c>bank-import.md</c> §3).</summary>
internal enum StatementSkipReason
{
    /// <summary>Pending or blocked funds; only booked rows become expenses.</summary>
    NotBooked,

    /// <summary>Money coming in: income and refunds are outside v1.</summary>
    Incoming,

    /// <summary>The row is in another currency than the Budget.</summary>
    CurrencyMismatch,

    /// <summary>Money moved between the user's own accounts or to a card; counting it would double-count spending.</summary>
    OwnTransfer,

    /// <summary>Cash taken out; cash is not spending until F2 cash accounts exist.</summary>
    CashWithdrawal,
}
