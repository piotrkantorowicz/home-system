namespace Notifications.Infrastructure.Channels;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Notifications.Application.Channels;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.SignalR;

internal sealed class WebSocketNotificationChannelSender(
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
            logger.LogDebug(
                "WebSocket sender: user {UserId} offline, leaving delivery {DeliveryId} pending",
                context.UserId, context.DeliveryId);
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

        logger.LogInformation(
            "WebSocket sender: pushed delivery {DeliveryId} to user {UserId}, awaiting ACK",
            context.DeliveryId, context.UserId);

        return DeliveryOutcome.Pending;
    }
}
