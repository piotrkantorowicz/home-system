namespace Investments.Domain.NetWorth;

using Investments.Domain.ValueObjects;

/// <summary>A wallet as far as net worth is concerned.</summary>
/// <param name="Id">The wallet.</param>
/// <param name="Kind">Its kind, for the breakdown.</param>
/// <param name="Currency">Its currency.</param>
/// <param name="OwnerPersonId">Its owner, for the breakdown.</param>
/// <param name="ArchivedOn">When archived, the date from which it no longer counts.</param>
public sealed record NetWorthWallet(WalletId Id, WalletKind Kind, WalletCurrency Currency, Guid OwnerPersonId, DateOnly? ArchivedOn = null);
