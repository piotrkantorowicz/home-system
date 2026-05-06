namespace Notifications.Application.Queries.ReplayPendingDeliveries;

public sealed record ReplayPendingDeliveryDto(
    Guid DeliveryId,
    Guid NotificationId,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt);
