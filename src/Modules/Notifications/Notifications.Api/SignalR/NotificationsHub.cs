namespace Notifications.Api.SignalR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Notifications.Application.Channels;
using Notifications.Application.Commands.AckNotificationDelivery;
using Notifications.Application.Queries.ReplayPendingDeliveries;
using Shared.Abstractions.Cqrs;

/// <summary>
/// SignalR hub behind the WebSocket channel. Tracks who is online, replays unacknowledged deliveries
/// on connect, and receives acknowledgements. Clients receive pushes on the <c>notification</c>
/// method with a <see cref="NotificationPayload"/>.
/// </summary>
/// <param name="registry">Tracks which users have open connections.</param>
/// <param name="queryDispatcher">Runs the pending-delivery replay query.</param>
/// <param name="commandDispatcher">Runs the acknowledge command.</param>
/// <param name="logger">Receives connection lifecycle events.</param>
[Authorize]
public sealed partial class NotificationsHub(
    INotificationConnectionRegistry registry,
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher,
    ILogger<NotificationsHub> logger) : Hub
{
    private const string ClientMethod = "notification";

    /// <summary>Registers the connection, then replays every pending delivery to the caller. Aborts connections without a user identifier.</summary>
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

    /// <summary>Unregisters the connection.</summary>
    /// <param name="exception">The reason the connection closed, if abnormal.</param>
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

    /// <summary>Client-invoked: confirms a pushed delivery was displayed so it is marked sent.</summary>
    /// <param name="deliveryId">The <see cref="NotificationPayload.DeliveryId"/> that was displayed.</param>
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

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Notifications hub connection rejected: no user identifier")]
    private partial void LogConnectionRejected();

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Notifications hub: user {UserId} connected ({ConnectionId})")]
    private partial void LogConnected(string userId, string connectionId);

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Notifications hub: user {UserId} disconnected ({ConnectionId})")]
    private partial void LogDisconnected(string userId, string connectionId);

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Notifications hub: replayed {Count} pending deliveries to user {UserId}")]
    private partial void LogReplayed(int count, string userId);
}
