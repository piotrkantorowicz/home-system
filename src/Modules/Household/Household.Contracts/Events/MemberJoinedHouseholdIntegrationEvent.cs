namespace Household.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>Raised when a person becomes a member of a household (including the owner at creation).</summary>
public sealed record MemberJoinedHouseholdIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid HouseholdId,
    Guid PersonId,
    string Role) : IIntegrationEvent;
