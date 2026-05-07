namespace Notifications.Application.Channels;

using Notifications.Domain.ValueObjects;

public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }

    Task<DeliveryOutcome> SendAsync(NotificationSendContext context, CancellationToken ct = default);
}
