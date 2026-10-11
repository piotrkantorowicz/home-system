namespace Investments.Application.Queries.GetWallet;

using Shared.Abstractions.Cqrs;

/// <summary>Reads one wallet the caller may see; not-found when it is missing or invisible.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The wallet.</param>
public sealed record GetWalletQuery(string AuthSubject, Guid Id) : IQuery<WalletDto>;
