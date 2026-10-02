namespace Budget.Application.Common;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;

/// <summary>The authenticated caller resolved against their current household.</summary>
/// <param name="HouseholdId">The caller's household.</param>
/// <param name="PersonId">The caller's person identifier — never taken from a request body.</param>
/// <param name="Role">The caller's household role name.</param>
/// <param name="ManagedPersonIds">Members currently managed (login-less) in the household; adults may act for them.</param>
internal sealed record BudgetCaller(Guid HouseholdId, Guid PersonId, string Role, IReadOnlyList<Guid> ManagedPersonIds)
{
    /// <summary>Whether the caller is an Owner or Adult — the only roles with shared-money access.</summary>
    public bool IsAdult => Role is "Owner" or "Adult";

    /// <summary>
    /// People whose personal envelopes the caller may see and manage: themselves, plus current
    /// managed members for an adult. Another independent person's envelopes are never included.
    /// </summary>
    public IReadOnlyList<Guid?> PersonalOwnerIds
        => IsAdult ? [PersonId, .. ManagedPersonIds.Cast<Guid?>()] : [PersonId];

    /// <summary>Whether the caller may see and manage the envelope. Callers must already be Owner/Adult/Child.</summary>
    /// <param name="account">The envelope.</param>
    public bool CanAccess(BudgetAccount account)
        => account.Visibility == AccountVisibility.Household
            ? IsAdult
            : PersonalOwnerIds.Contains(account.OwnerPersonId);
}
