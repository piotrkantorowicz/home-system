namespace Notifications.Application.EventHandlers;

using System.Globalization;
using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class MealReminderDueIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<MealReminderDueIntegrationEvent>
{
    public Task HandleAsync(MealReminderDueIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var placeholders = new Dictionary<string, string>
        {
            ["MealSlotName"] = @event.MealSlotName,
            ["PlannedAt"] = @event.PlannedAt.ToString("HH:mm", CultureInfo.InvariantCulture)
        };

        var payload = JsonSerializer.Serialize(new
        {
            @event.MealEntryId,
            @event.MealSlotName,
            @event.PlannedAt
        });

        return dispatcher.DispatchAsync(
            NotificationType.MealReminder,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
