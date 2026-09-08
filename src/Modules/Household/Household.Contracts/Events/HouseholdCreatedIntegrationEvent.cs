namespace Household.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>Raised once when a household is created, before its first member events.</summary>
public sealed record HouseholdCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid HouseholdId,
    Guid OwnerPersonId) : IIntegrationEvent;
