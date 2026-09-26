namespace Notifications.Application.Queries.ListDeadLetterDeliveries;

/// <summary>
/// A dead-lettered delivery with enough of its notification to recognise it.
/// </summary>
/// <param name="DeliveryId">The delivery; pass it to retry.</param>
/// <param name="NotificationId">The notification being delivered.</param>
/// <param name="UserId">Auth subject of the recipient.</param>
/// <param name="Type">Notification type name.</param>
/// <param name="Title">Rendered title.</param>
/// <param name="Channel">Channel name.</param>
/// <param name="AttemptCount">Attempts made.</param>
/// <param name="LastAttemptAt">Time of the last attempt, UTC.</param>
/// <param name="FailureReason">Why the last attempt failed.</param>
public sealed record DeadLetterDeliveryDto(
    Guid DeliveryId,
    Guid NotificationId,
    string UserId,
    string Type,
    string Title,
    string Channel,
    int AttemptCount,
    DateTime? LastAttemptAt,
    string? FailureReason);
