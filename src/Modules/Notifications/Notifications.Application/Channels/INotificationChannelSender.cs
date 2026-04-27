namespace Notifications.Application.Channels;

using Notifications.Domain.ValueObjects;

public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }
    Task SendAsync(string userId, string title, string body, CancellationToken ct = default);
}
