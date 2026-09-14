namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when a household is created, alongside the owner's <see cref="MemberJoinedHouseholdDomainEvent"/>.
/// </summary>
/// <param name="HouseholdId">The new household.</param>
/// <param name="OwnerPersonId">Its first owner.</param>
public sealed record HouseholdCreatedDomainEvent(
    HouseholdId HouseholdId,
    PersonId OwnerPersonId) : IDomainEvent;
