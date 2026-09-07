namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record MemberJoinedHouseholdDomainEvent(
    HouseholdId HouseholdId,
    PersonId PersonId,
    HouseholdRole Role) : IDomainEvent;
