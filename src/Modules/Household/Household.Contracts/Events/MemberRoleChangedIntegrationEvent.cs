namespace Household.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>Raised when an existing member's role changes.</summary>
public sealed record MemberRoleChangedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid HouseholdId,
    Guid PersonId,
    string PreviousRole,
    string NewRole) : IIntegrationEvent;
