namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when a person becomes a member — on household creation, on adding an existing or managed person, or on accepting an invitation.
/// </summary>
/// <param name="HouseholdId">The household joined.</param>
/// <param name="PersonId">The new member.</param>
/// <param name="Role">Their initial role.</param>
public sealed record MemberJoinedHouseholdDomainEvent(
    HouseholdId HouseholdId,
    PersonId PersonId,
    HouseholdRole Role) : IDomainEvent;
