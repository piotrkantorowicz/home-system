namespace Notifications.Application.Channels;

using Notifications.Domain.ValueObjects;

/// <summary>
/// Everything a channel sender needs to deliver one notification, without touching storage.
/// </summary>
/// <param name="DeliveryId">The delivery row this send belongs to; WebSocket clients acknowledge with it.</param>
/// <param name="NotificationId">The notification.</param>
/// <param name="UserId">Auth subject of the recipient.</param>
/// <param name="Type">Notification type.</param>
/// <param name="Title">Rendered title.</param>
/// <param name="Body">Rendered body.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
public sealed record NotificationSendContext(
    Guid DeliveryId,
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    string Title,
    string Body,
    DateTime CreatedAt);
