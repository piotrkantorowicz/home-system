namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class GoalMilestoneReachedIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<GoalMilestoneReachedIntegrationEvent>
{
    public Task HandleAsync(GoalMilestoneReachedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var placeholders = new Dictionary<string, string>
        {
            ["MilestoneLabel"] = @event.MilestoneLabel,
            ["GoalKind"] = @event.GoalKind
        };

        var payload = JsonSerializer.Serialize(new
        {
            @event.GoalKind,
            @event.MilestoneLabel,
            @event.Value
        });

        return dispatcher.DispatchAsync(
            NotificationType.GoalMilestone,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
