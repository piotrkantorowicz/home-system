namespace Budget.Application.Common;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Household.Contracts.Interfaces;

/// <summary>The authenticated caller resolved against their current household.</summary>
/// <param name="HouseholdId">The caller's household.</param>
/// <param name="PersonId">The caller's person identifier — never taken from a request body.</param>
/// <param name="Role">The caller's household role name.</param>
/// <param name="Members">The household roster as of this request.</param>
internal sealed record BudgetCaller(Guid HouseholdId, Guid PersonId, string Role, IReadOnlyList<HouseholdContextMember> Members)
{
    /// <summary>Members currently managed (login-less); adults may act for them.</summary>
    public IEnumerable<Guid> ManagedPersonIds => Members.Where(m => m.IsManaged).Select(m => m.PersonId);

    /// <summary>The caller as a name-snapshotted reference.</summary>
    public PersonRef Self => new(PersonId, Members.FirstOrDefault(m => m.PersonId == PersonId)?.DisplayName ?? "Unknown");

    /// <summary>Resolves a current Owner/Adult member (eligible payer or participant), or <see langword="null"/>.</summary>
    /// <param name="personId">The person.</param>
    public PersonRef? FindAdult(Guid personId)
        => Members.FirstOrDefault(m => m.PersonId == personId && m.Role is "Owner" or "Adult") is { } m
            ? new PersonRef(m.PersonId, m.DisplayName)
            : null;

    /// <summary>Resolves any current member (e.g. a personal envelope's owner), or <see langword="null"/>.</summary>
    /// <param name="personId">The person.</param>
    public PersonRef? FindMember(Guid personId)
        => Members.FirstOrDefault(m => m.PersonId == personId) is { } m ? new PersonRef(m.PersonId, m.DisplayName) : null;

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
