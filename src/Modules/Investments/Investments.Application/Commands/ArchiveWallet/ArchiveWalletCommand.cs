namespace Investments.Application.Commands.ArchiveWallet;

using Investments.Application.Queries.GetWallet;
using Shared.Abstractions.Cqrs;

/// <summary>Archives a wallet the caller may edit; history stays. Repeating is a no-op.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The wallet.</param>
public sealed record ArchiveWalletCommand(string AuthSubject, Guid Id) : ICommand<WalletDto>;
