namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>
/// Published by the meal reminder job when a planned meal is within the user's lead time. Sent at
/// most once per meal entry. Consumed by Notifications.
/// </summary>
/// <param name="EventId">Unique identity of this occurrence; consumers de-duplicate on it.</param>
/// <param name="OccurredAt">When the event was raised, UTC.</param>
/// <param name="UserId">Auth subject of the user the notification is for.</param>
/// <param name="Locale">The user's locale (e.g. <c>en</c>, <c>pl</c>) for rendering the notification text.</param>
/// <param name="MealEntryId">The upcoming meal entry.</param>
/// <param name="MealSlotName">Display name of the slot, e.g. "Lunch".</param>
/// <param name="PlannedAt">When the meal is planned, UTC.</param>
public sealed record MealReminderDueIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale,
    Guid MealEntryId,
    string MealSlotName,
    DateTime PlannedAt) : IIntegrationEvent;
