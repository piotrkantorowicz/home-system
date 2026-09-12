namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class WeeklySummaryDueIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<WeeklySummaryDueIntegrationEvent>
{
    public Task HandleAsync(WeeklySummaryDueIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var payload = JsonSerializer.Serialize(new
        {
            @event.WeekStart,
            @event.WeekEnd,
            @event.TotalKcal,
            @event.TargetKcal,
            @event.AvgWaterLiters,
            @event.WeightDeltaKg,
            @event.MealsCompleted,
            @event.MealsPlanned
        });

        var placeholders = new Dictionary<string, string>
        {
            ["TotalKcal"] = @event.TotalKcal.ToString(),
            ["TargetKcal"] = @event.TargetKcal.ToString(),
            ["MealsCompleted"] = @event.MealsCompleted.ToString(),
            ["MealsPlanned"] = @event.MealsPlanned.ToString(),
        };

        return dispatcher.DispatchAsync(
            NotificationType.WeeklySummary,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
