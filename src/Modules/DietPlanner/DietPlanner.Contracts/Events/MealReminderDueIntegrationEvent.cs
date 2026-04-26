namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

public sealed record MealReminderDueIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale,
    Guid MealEntryId,
    string MealSlotName,
    DateTime PlannedAt) : IIntegrationEvent;
