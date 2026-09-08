namespace Household.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>Raised when a person is removed from, or leaves, a household.</summary>
public sealed record MemberLeftHouseholdIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid HouseholdId,
    Guid PersonId) : IIntegrationEvent;
