namespace Notifications.Application.Queries.ReplayPendingDeliveries;

/// <summary>
/// A pending WebSocket delivery with the notification content to push.
/// </summary>
/// <param name="DeliveryId">The delivery to acknowledge after display.</param>
/// <param name="NotificationId">The notification.</param>
/// <param name="Type">Notification type name.</param>
/// <param name="Title">Rendered title.</param>
/// <param name="Body">Rendered body.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
public sealed record ReplayPendingDeliveryDto(
    Guid DeliveryId,
    Guid NotificationId,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt);
