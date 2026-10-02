namespace Budget.Application.Queries.GetSettlement;

/// <summary>
/// Who owes whom across every active recorded entry — no date cutoff; future-dated records count
/// immediately. Personal, household-funded and voided expenses contribute nothing.
/// </summary>
/// <param name="Currency">The budget's currency.</param>
/// <param name="IsSettled">Every balance is zero.</param>
/// <param name="Balances">One row per person with a ledger history, largest credit first.</param>
/// <param name="Suggestions">Deterministic payments that bring every balance to zero.</param>
public sealed record SettlementDto(
    string Currency,
    bool IsSettled,
    IReadOnlyList<SettlementBalanceDto> Balances,
    IReadOnlyList<SettlementTransferDto> Suggestions);
