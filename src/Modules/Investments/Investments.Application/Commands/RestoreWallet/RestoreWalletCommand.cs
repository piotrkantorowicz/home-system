namespace Investments.Application.Commands.RestoreWallet;

using Investments.Application.Queries.GetWallet;
using Shared.Abstractions.Cqrs;

/// <summary>Restores an archived wallet the caller may edit. Repeating is a no-op.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The wallet.</param>
public sealed record RestoreWalletCommand(string AuthSubject, Guid Id) : ICommand<WalletDto>;
