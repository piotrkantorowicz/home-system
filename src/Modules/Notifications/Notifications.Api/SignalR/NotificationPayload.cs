namespace Notifications.Api.SignalR;

/// <summary>
/// What the hub pushes to the browser for one delivery; the client calls <c>Acknowledge</c> with <paramref name="DeliveryId"/> once displayed.
/// </summary>
/// <param name="DeliveryId">The delivery to acknowledge.</param>
/// <param name="NotificationId">The notification.</param>
/// <param name="Type">Notification type name.</param>
/// <param name="Title">Rendered title.</param>
/// <param name="Body">Rendered body.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
public sealed record NotificationPayload(
    Guid DeliveryId,
    Guid NotificationId,
    string Type,
    string Title,
    string Body,
    DateTime CreatedAt);
