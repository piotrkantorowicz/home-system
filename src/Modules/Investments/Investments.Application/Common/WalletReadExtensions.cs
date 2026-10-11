namespace Investments.Application.Common;

using System.Linq.Expressions;
using Investments.Application.Queries.GetWallet;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;

/// <summary>
/// The one place wallet visibility is applied to queries — before filtering, counting and lookup —
/// and the one wallet projection.
/// </summary>
internal static class WalletReadExtensions
{
    /// <summary>Wallets in the caller's household that the caller may see (same rule as <see cref="InvestmentsCaller.CanAccess"/>).</summary>
    public static IQueryable<Wallet> VisibleTo(this IQueryable<Wallet> wallets, InvestmentsCaller caller)
    {
        var householdId = caller.HouseholdId;
        var managedOwners = caller.ManagedOwnerIds;
        var members = caller.MemberIds;
        var seesShared = caller.IsAdult;

        return wallets.Where(w => w.HouseholdId == householdId
            && (managedOwners.Contains(w.OwnerPersonId)
                || (seesShared && w.Visibility == WalletVisibility.Shared && members.Contains(w.OwnerPersonId))));
    }

    public static readonly Expression<Func<Wallet, WalletRow>> ToRow = w => new WalletRow(
        w.Id.Value, w.Name, w.Institution, w.Kind, w.Currency, w.Visibility, w.OwnerPersonId, w.OwnerName, w.IsArchived, w.Version, w.CreatedAt);

    public static WalletDto ToDto(this WalletRow r, InvestmentsCaller caller) => new(
        r.Id, r.Name, r.Institution.ToString(), r.Kind.ToString(), r.Currency.ToString(), r.Visibility.ToString(),
        r.OwnerPersonId, caller.NameOf(r.OwnerPersonId) ?? r.OwnerName, r.IsArchived, r.Version, r.CreatedAt,
        caller.CanChangeVisibility(r.OwnerPersonId));

    public static WalletDto ToDto(this Wallet w, InvestmentsCaller caller) => new WalletRow(
        w.Id.Value, w.Name, w.Institution, w.Kind, w.Currency, w.Visibility, w.OwnerPersonId, w.OwnerName, w.IsArchived, w.Version, w.CreatedAt)
        .ToDto(caller);
}
