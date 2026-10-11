namespace Investments.Application.Common;

using Household.Contracts.Interfaces;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;

/// <summary>The authenticated caller resolved against their current household.</summary>
/// <param name="HouseholdId">The caller's household.</param>
/// <param name="PersonId">The caller's person identifier — never taken from a request body.</param>
/// <param name="Role">The caller's household role name.</param>
/// <param name="Members">The household roster as of this request.</param>
internal sealed record InvestmentsCaller(Guid HouseholdId, Guid PersonId, string Role, IReadOnlyList<HouseholdContextMember> Members)
{
    /// <summary>Whether the caller is an Owner or Adult — the only roles with shared-money access.</summary>
    public bool IsAdult => Role is "Owner" or "Adult";

    /// <summary>Every current member; a departed owner's wallets drop out of household views.</summary>
    public IReadOnlyList<Guid> MemberIds => [.. Members.Select(m => m.PersonId)];

    /// <summary>
    /// Owners whose wallets the caller may see and manage regardless of visibility: themselves, plus
    /// current managed members for an adult. Another independent person is never included.
    /// </summary>
    public IReadOnlyList<Guid> ManagedOwnerIds
        => IsAdult ? [PersonId, .. Members.Where(m => m.IsManaged).Select(m => m.PersonId)] : [PersonId];

    /// <summary>Current display name of a member, or <see langword="null"/> when they are not on the roster.</summary>
    /// <param name="personId">The person.</param>
    public string? NameOf(Guid personId) => Members.FirstOrDefault(m => m.PersonId == personId)?.DisplayName;

    /// <summary>Whether the caller may see and edit the wallet.</summary>
    /// <param name="wallet">A wallet in the caller's household.</param>
    public bool CanAccess(Wallet wallet)
        => wallet.HouseholdId == HouseholdId
           && (ManagedOwnerIds.Contains(wallet.OwnerPersonId)
               || (IsAdult && wallet.Visibility == WalletVisibility.Shared && MemberIds.Contains(wallet.OwnerPersonId)));

    /// <summary>Whether the caller may change the wallet's visibility: its owner, or an adult managing the owner.</summary>
    /// <param name="ownerPersonId">The wallet's owner.</param>
    public bool CanChangeVisibility(Guid ownerPersonId) => ManagedOwnerIds.Contains(ownerPersonId);
}
