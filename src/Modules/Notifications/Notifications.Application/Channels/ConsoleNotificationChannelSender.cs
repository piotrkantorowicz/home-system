namespace Notifications.Application.Channels;

using Microsoft.Extensions.Logging;
using Notifications.Domain.ValueObjects;

internal sealed class ConsoleNotificationChannelSender(
    ILogger<ConsoleNotificationChannelSender> logger)
    : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.Console;

    public Task<DeliveryOutcome> SendAsync(NotificationSendContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        logger.LogInformation(
            "[Notification] user={UserId} delivery={DeliveryId} channel=Console title=\"{Title}\" body=\"{Body}\"",
            context.UserId, context.DeliveryId, context.Title, context.Body);

        return Task.FromResult(DeliveryOutcome.Sent);
    }
}
