namespace Investments.Application.Commands.CreateWallet;

using Investments.Application.Queries.GetWallet;
using Shared.Abstractions.Cqrs;

/// <summary>Creates a wallet in the caller's household.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Name">Display name, 1–80 characters.</param>
/// <param name="Institution">Institution name, e.g. <c>XTB</c>.</param>
/// <param name="Kind">Account kind, e.g. <c>IKE</c>.</param>
/// <param name="Currency"><c>PLN</c>, <c>EUR</c> or <c>USD</c>.</param>
/// <param name="Visibility"><c>Shared</c> or <c>Private</c>.</param>
/// <param name="OwnerPersonId">The caller (default) or, for an adult, a current managed member.</param>
public sealed record CreateWalletCommand(
    string AuthSubject,
    string Name,
    string Institution,
    string Kind,
    string Currency,
    string Visibility,
    Guid? OwnerPersonId) : ICommand<WalletDto>;
