namespace Budget.Application.Queries.GetSettlement;

/// <summary>One person's cumulative balance.</summary>
/// <param name="PersonId">The person.</param>
/// <param name="DisplayName">Current roster name for a member, else the latest name stored in this ledger.</param>
/// <param name="IsFormerAdult">No longer a current owner or adult here (left, or demoted); their balance stays outstanding.</param>
/// <param name="Net">Decimal string: positive = should receive, negative = owes.</param>
public sealed record SettlementBalanceDto(Guid PersonId, string DisplayName, bool IsFormerAdult, string Net);
