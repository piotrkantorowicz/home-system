namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class MealMissedIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<MealMissedIntegrationEvent>
{
    public Task HandleAsync(MealMissedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var placeholders = new Dictionary<string, string>
        {
            ["MealSlotName"] = @event.MealSlotName,
            ["PlannedAt"] = @event.PlannedAt.ToString("HH:mm")
        };

        var payload = JsonSerializer.Serialize(new
        {
            @event.MealEntryId,
            @event.MealSlotName,
            @event.PlannedAt
        });

        return dispatcher.DispatchAsync(
            NotificationType.MealMissed,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
