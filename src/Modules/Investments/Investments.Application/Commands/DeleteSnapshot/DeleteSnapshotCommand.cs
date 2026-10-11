namespace Investments.Application.Commands.DeleteSnapshot;

using Shared.Abstractions.Cqrs;

/// <summary>Removes a wallet's balance for one date. Allowed on archived wallets.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="WalletId">A wallet the caller may see.</param>
/// <param name="AsOf">The date; not-found when nothing is recorded for it.</param>
public sealed record DeleteSnapshotCommand(string AuthSubject, Guid WalletId, DateOnly AsOf) : ICommand;
