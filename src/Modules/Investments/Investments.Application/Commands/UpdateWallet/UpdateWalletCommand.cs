namespace Investments.Application.Commands.UpdateWallet;

using Investments.Application.Queries.GetWallet;
using Shared.Abstractions.Cqrs;

/// <summary>Renames a wallet and/or changes its visibility. Owner, kind and currency cannot change.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The wallet.</param>
/// <param name="Name">New display name, 1–80 characters.</param>
/// <param name="Visibility"><c>Shared</c> or <c>Private</c>; changing it needs the owner or a managing adult.</param>
/// <param name="Version">The concurrency token the caller last saw; a mismatch is a 409.</param>
public sealed record UpdateWalletCommand(string AuthSubject, Guid Id, string Name, string Visibility, uint Version)
    : ICommand<WalletDto>;
