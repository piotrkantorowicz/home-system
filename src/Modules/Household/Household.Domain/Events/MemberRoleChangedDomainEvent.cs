namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record MemberRoleChangedDomainEvent(
    HouseholdId HouseholdId,
    PersonId PersonId,
    HouseholdRole PreviousRole,
    HouseholdRole NewRole) : IDomainEvent;
