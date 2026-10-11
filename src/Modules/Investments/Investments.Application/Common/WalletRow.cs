namespace Investments.Application.Common;

using Investments.Domain.ValueObjects;

/// <summary>A wallet projected from the database, before roster names and permissions are applied.</summary>
internal sealed record WalletRow(
    Guid Id,
    string Name,
    WalletInstitution Institution,
    WalletKind Kind,
    WalletCurrency Currency,
    WalletVisibility Visibility,
    Guid OwnerPersonId,
    string OwnerName,
    bool IsArchived,
    uint Version,
    DateTimeOffset CreatedAt,
    decimal? LatestBalance,
    DateOnly? LatestAsOf);
