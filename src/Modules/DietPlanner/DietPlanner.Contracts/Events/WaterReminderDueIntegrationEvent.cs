namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>
/// Published by the water reminder job when the user's configured interval has elapsed inside their
/// reminder window and water tracking is on. Consumed by Notifications.
/// </summary>
/// <param name="EventId">Unique identity of this occurrence; consumers de-duplicate on it.</param>
/// <param name="OccurredAt">When the event was raised, UTC.</param>
/// <param name="UserId">Auth subject of the user the notification is for.</param>
/// <param name="Locale">The user's locale (e.g. <c>en</c>, <c>pl</c>) for rendering the notification text.</param>
public sealed record WaterReminderDueIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale) : IIntegrationEvent;
