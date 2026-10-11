namespace Investments.Application.Common;

using Investments.Domain.NetWorth;
using Investments.Domain.ValueObjects;

/// <summary>A wallet projected for net worth, with the names the dashboard shows.</summary>
internal sealed record NetWorthWalletRow(
    WalletId Id,
    string Name,
    WalletKind Kind,
    WalletCurrency Currency,
    Guid OwnerPersonId,
    string OwnerName,
    DateOnly? ArchivedOn)
{
    public NetWorthWallet ToNetWorthWallet() => new(Id, Kind, Currency, OwnerPersonId, ArchivedOn);
}
