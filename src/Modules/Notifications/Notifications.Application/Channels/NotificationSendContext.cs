namespace Notifications.Application.Channels;

using Notifications.Domain.ValueObjects;

public sealed record NotificationSendContext(
    Guid DeliveryId,
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    string Title,
    string Body,
    DateTime CreatedAt);
