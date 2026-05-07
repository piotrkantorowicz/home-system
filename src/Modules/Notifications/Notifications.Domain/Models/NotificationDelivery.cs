namespace Notifications.Domain.Models;

using Notifications.Domain.ValueObjects;

public sealed class NotificationDelivery
{
    private NotificationDelivery() { }

    public static NotificationDelivery Create(
        NotificationDeliveryId id,
        NotificationId notificationId,
        NotificationChannel channel)
        => new()
        {
            Id = id,
            NotificationId = notificationId,
            Channel = channel,
            Status = DeliveryStatus.Pending,
            AttemptCount = 0,
        };

    public NotificationDeliveryId Id { get; private set; } = default!;
    public NotificationId NotificationId { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkSent(DateTime utcNow)
    {
        if (Status == DeliveryStatus.Sent) return;

        Status = DeliveryStatus.Sent;
        SentAt = utcNow;
        LastAttemptAt = utcNow;
        AttemptCount++;
    }

    public void RecordPendingAttempt(DateTime utcNow)
    {
        Status = DeliveryStatus.Pending;
        LastAttemptAt = utcNow;
        AttemptCount++;
    }

    public void MarkFailed(DateTime utcNow, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = DeliveryStatus.Failed;
        LastAttemptAt = utcNow;
        FailureReason = reason;
        AttemptCount++;
    }

    public void MarkSkipped(DateTime utcNow)
    {
        Status = DeliveryStatus.Skipped;
        LastAttemptAt = utcNow;
    }
}
