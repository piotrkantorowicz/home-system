namespace Budget.Application.Queries.GetSettlement;

using Shared.Abstractions.Cqrs;

/// <summary>Reads the household's outstanding balances. Owner/Adult only; Child and Guest are denied.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record GetSettlementQuery(string AuthSubject) : IQuery<SettlementDto>;
