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

        LogNotification(context.UserId, context.DeliveryId, context.Title, context.Body);

        return Task.FromResult(DeliveryOutcome.Sent);
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "[Notification] user={UserId} delivery={DeliveryId} channel=Console title=\"{Title}\" body=\"{Body}\"")]
    private partial void LogNotification(string userId, Guid deliveryId, string title, string body);
}
