namespace Notifications.Api.Channels;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Notifications.Api.SignalR;
using Notifications.Application.Channels;
using Notifications.Domain.ValueObjects;

internal sealed partial class WebSocketNotificationChannelSender(
    IHubContext<NotificationsHub> hubContext,
    INotificationConnectionRegistry registry,
    ILogger<WebSocketNotificationChannelSender> logger)
    : INotificationChannelSender
{
    private const string ClientMethod = "notification";

    public NotificationChannel Channel => NotificationChannel.WebSocket;

    public async Task<DeliveryOutcome> SendAsync(NotificationSendContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!registry.IsOnline(context.UserId))
        {
            LogUserOffline(context.UserId, context.DeliveryId);
            return DeliveryOutcome.Pending;
        }

        var payload = new NotificationPayload(
            DeliveryId: context.DeliveryId,
            NotificationId: context.NotificationId,
            Type: context.Type.ToString(),
            Title: context.Title,
            Body: context.Body,
            CreatedAt: context.CreatedAt);

        await hubContext.Clients.User(context.UserId)
            .SendAsync(ClientMethod, payload, ct)
            .ConfigureAwait(false);

        LogPushed(context.DeliveryId, context.UserId);

        return DeliveryOutcome.Pending;
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "WebSocket sender: user {UserId} offline, leaving delivery {DeliveryId} pending")]
    private partial void LogUserOffline(string userId, Guid deliveryId);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "WebSocket sender: pushed delivery {DeliveryId} to user {UserId}, awaiting ACK")]
    private partial void LogPushed(Guid deliveryId, string userId);
}
