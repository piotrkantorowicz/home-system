namespace Budget.Application.Queries.GetSettlement;

/// <summary>A suggested payment. It is a proposal only — nothing moves money.</summary>
/// <param name="FromPersonId">Who would pay.</param>
/// <param name="FromDisplayName">Name of the payer.</param>
/// <param name="ToPersonId">Who would receive.</param>
/// <param name="ToDisplayName">Name of the recipient.</param>
/// <param name="Amount">Positive decimal string with two decimals.</param>
public sealed record SettlementTransferDto(Guid FromPersonId, string FromDisplayName, Guid ToPersonId, string ToDisplayName, string Amount);
