namespace Notifications.Infrastructure.SignalR;

public sealed record NotificationPayload(
    Guid DeliveryId,
    Guid NotificationId,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt);
