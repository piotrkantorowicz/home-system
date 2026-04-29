namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class WaterReminderDueIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<WaterReminderDueIntegrationEvent>
{
    public Task HandleAsync(WaterReminderDueIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var payload = JsonSerializer.Serialize(new { @event.UserId });

        return dispatcher.DispatchAsync(
            NotificationType.WaterReminder,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders: new Dictionary<string, string>(),
            ct);
    }
}
