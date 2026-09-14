namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when a member is removed or leaves.
/// </summary>
/// <param name="HouseholdId">The household left.</param>
/// <param name="PersonId">The former member.</param>
public sealed record MemberLeftHouseholdDomainEvent(
    HouseholdId HouseholdId,
    PersonId PersonId) : IDomainEvent;
