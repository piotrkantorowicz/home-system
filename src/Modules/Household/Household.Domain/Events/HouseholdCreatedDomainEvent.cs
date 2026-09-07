namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record HouseholdCreatedDomainEvent(
    HouseholdId HouseholdId,
    PersonId OwnerPersonId) : IDomainEvent;
