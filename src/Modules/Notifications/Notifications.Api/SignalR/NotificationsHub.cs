namespace Notifications.Api.SignalR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Notifications.Application.Channels;
using Notifications.Application.Commands.AckNotificationDelivery;
using Notifications.Application.Queries.ReplayPendingDeliveries;
using Shared.Abstractions.Cqrs;

[Authorize]
public sealed partial class NotificationsHub(
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
            LogConnectionRejected();
            Context.Abort();
            return;
        }

        registry.Track(userId);
        LogConnected(userId, Context.ConnectionId);

        await base.OnConnectedAsync();
        await ReplayPendingAsync(userId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            registry.Untrack(userId);
            LogDisconnected(userId, Context.ConnectionId);
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

        LogReplayed(pending.Count, userId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notifications hub connection rejected: no user identifier")]
    private partial void LogConnectionRejected();

    [LoggerMessage(Level = LogLevel.Information, Message = "Notifications hub: user {UserId} connected ({ConnectionId})")]
    private partial void LogConnected(string userId, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notifications hub: user {UserId} disconnected ({ConnectionId})")]
    private partial void LogDisconnected(string userId, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notifications hub: replayed {Count} pending deliveries to user {UserId}")]
    private partial void LogReplayed(int count, string userId);
}
