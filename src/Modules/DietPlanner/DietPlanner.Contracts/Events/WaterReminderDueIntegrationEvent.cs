namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

public sealed record WaterReminderDueIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale) : IIntegrationEvent;
