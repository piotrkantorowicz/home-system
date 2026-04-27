namespace Notifications.Application.Channels;

using Microsoft.Extensions.Logging;
using Notifications.Domain.ValueObjects;

internal sealed class ConsoleNotificationChannelSender(
    ILogger<ConsoleNotificationChannelSender> logger)
    : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.Console;

    public Task SendAsync(string userId, string title, string body, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        logger.LogInformation(
            "[Notification] user={UserId} channel=Console title=\"{Title}\" body=\"{Body}\"",
            userId, title, body);

        return Task.CompletedTask;
    }
}
