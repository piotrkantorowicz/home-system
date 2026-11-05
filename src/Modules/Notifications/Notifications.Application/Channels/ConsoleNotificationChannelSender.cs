namespace Notifications.Application.Channels;

using Microsoft.Extensions.Logging;
using Notifications.Domain.ValueObjects;

internal sealed partial class ConsoleNotificationChannelSender(
    ILogger<ConsoleNotificationChannelSender> logger)
    : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.Console;

    public Task<DeliveryOutcome> SendAsync(NotificationSendContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        LogNotification(context.UserId, context.DeliveryId);

        return Task.FromResult(DeliveryOutcome.Sent);
    }

    // IDs only: log placeholders are exported as OTLP attributes (docs/rules/backend-observability.md),
    // so the notification's title and body stay out of the log.
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "[Notification] user={UserId} delivery={DeliveryId} channel=Console")]
    private partial void LogNotification(string userId, Guid deliveryId);
}
