namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>
/// Published once when a user's goal milestone is first reached — today only the target weight,
/// evaluated after every weight entry. Consumed by Notifications to send a goal alert.
/// </summary>
/// <param name="EventId">Unique identity of this occurrence; consumers de-duplicate on it.</param>
/// <param name="OccurredAt">When the event was raised, UTC.</param>
/// <param name="UserId">Auth subject of the user the notification is for.</param>
/// <param name="Locale">The user's locale (e.g. <c>en</c>, <c>pl</c>) for rendering the notification text.</param>
/// <param name="GoalKind">Which goal was reached; currently always <c>WeightTarget</c>.</param>
/// <param name="MilestoneLabel">Human-readable description of the milestone for the notification body.</param>
/// <param name="Value">The value reached (target weight in kilograms), if applicable.</param>
public sealed record GoalMilestoneReachedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale,
    string GoalKind,
    string MilestoneLabel,
    decimal? Value) : IIntegrationEvent;
