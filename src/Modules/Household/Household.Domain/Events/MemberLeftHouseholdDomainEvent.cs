namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record MemberLeftHouseholdDomainEvent(
    HouseholdId HouseholdId,
    PersonId PersonId) : IDomainEvent;
