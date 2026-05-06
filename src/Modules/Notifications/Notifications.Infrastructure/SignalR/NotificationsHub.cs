namespace Notifications.Infrastructure.SignalR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Notifications.Application.Channels;
using Notifications.Application.Commands.AckNotificationDelivery;
using Notifications.Application.Queries.ReplayPendingDeliveries;
using Shared.Abstractions.Cqrs;

[Authorize]
public sealed class NotificationsHub(
    INotificationConnectionRegistry registry,
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher,
    ILogger<NotificationsHub> logger) : Hub
{
    private const string ClientMethod = "notification";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Notifications hub connection rejected: no user identifier");
            Context.Abort();
            return;
        }

        registry.Track(userId);
        logger.LogInformation("Notifications hub: user {UserId} connected ({ConnectionId})",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
        await ReplayPendingAsync(userId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            registry.Untrack(userId);
            logger.LogInformation("Notifications hub: user {UserId} disconnected ({ConnectionId})",
                userId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public Task Acknowledge(Guid deliveryId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;

        return commandDispatcher.SendAsync(
            new AckNotificationDeliveryCommand(deliveryId, userId),
            Context.ConnectionAborted);
    }

    private async Task ReplayPendingAsync(string userId)
    {
        var pending = await queryDispatcher.SendAsync<
            ReplayPendingWebSocketDeliveriesQuery,
            IReadOnlyList<ReplayPendingDeliveryDto>>(
            new ReplayPendingWebSocketDeliveriesQuery(userId),
            Context.ConnectionAborted);

        if (pending.Count == 0) return;

        foreach (var row in pending)
        {
            var payload = new NotificationPayload(
                DeliveryId: row.DeliveryId,
                NotificationId: row.NotificationId,
                Type: row.Type,
                Title: row.Title,
                Body: row.Body,
                CreatedAt: row.CreatedAt);

            await Clients.Caller.SendAsync(ClientMethod, payload, Context.ConnectionAborted);
        }

        logger.LogInformation(
            "Notifications hub: replayed {Count} pending deliveries to user {UserId}",
            pending.Count, userId);
    }
}
