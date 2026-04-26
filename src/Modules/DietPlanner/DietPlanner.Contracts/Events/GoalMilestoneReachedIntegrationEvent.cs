namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

public sealed record GoalMilestoneReachedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale,
    string GoalKind,
    string MilestoneLabel,
    decimal? Value) : IIntegrationEvent;
