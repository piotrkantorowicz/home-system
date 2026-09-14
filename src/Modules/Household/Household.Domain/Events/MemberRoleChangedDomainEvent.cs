namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when a member's role actually changes (not on a same-role no-op).
/// </summary>
/// <param name="HouseholdId">The household.</param>
/// <param name="PersonId">The member.</param>
/// <param name="PreviousRole">The role before the change.</param>
/// <param name="NewRole">The role after the change.</param>
public sealed record MemberRoleChangedDomainEvent(
    HouseholdId HouseholdId,
    PersonId PersonId,
    HouseholdRole PreviousRole,
    HouseholdRole NewRole) : IDomainEvent;
