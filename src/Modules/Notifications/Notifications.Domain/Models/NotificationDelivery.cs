namespace Notifications.Domain.Models;

using Notifications.Domain.ValueObjects;

/// <summary>
/// One attempt-tracked delivery of a <see cref="Notification"/> over one channel. Starts
/// <see cref="DeliveryStatus.Pending"/> with zero attempts; the dispatcher and retry worker move it
/// through the other states. Every transition except <see cref="MarkSkipped"/> counts an attempt.
/// </summary>
public sealed class NotificationDelivery
{
    private NotificationDelivery() { }

    /// <summary>Creates a pending delivery with no attempts.</summary>
    /// <param name="id">Identifier for the new delivery.</param>
    /// <param name="notificationId">The notification being delivered.</param>
    /// <param name="channel">The channel to deliver over.</param>
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

    /// <summary>Identifier.</summary>
    public NotificationDeliveryId Id { get; private set; } = default!;
    /// <summary>The notification being delivered.</summary>
    public NotificationId NotificationId { get; private set; } = default!;
    /// <summary>The channel this delivery uses.</summary>
    public NotificationChannel Channel { get; private set; }
    /// <summary>Current state.</summary>
    public DeliveryStatus Status { get; private set; }
    /// <summary>How many send attempts have been made.</summary>
    public int AttemptCount { get; private set; }
    /// <summary>Time of the most recent attempt, UTC; <see langword="null"/> before the first.</summary>
    public DateTime? LastAttemptAt { get; private set; }
    /// <summary>When delivery succeeded, UTC; <see langword="null"/> until then.</summary>
    public DateTime? SentAt { get; private set; }
    /// <summary>Why the last attempt failed, if it did.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>Records a successful delivery. Idempotent once sent.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    public void MarkSent(DateTime utcNow)
    {
        if (Status == DeliveryStatus.Sent) return;

        Status = DeliveryStatus.Sent;
        SentAt = utcNow;
        LastAttemptAt = utcNow;
        AttemptCount++;
    }

    /// <summary>Records an attempt that handed the message off but has no confirmation yet (e.g. pushed over WebSocket, awaiting ACK).</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    public void RecordPendingAttempt(DateTime utcNow)
    {
        Status = DeliveryStatus.Pending;
        LastAttemptAt = utcNow;
        AttemptCount++;
    }

    /// <summary>Records a failed attempt; the retry worker may try again.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    /// <param name="reason">Why it failed; required.</param>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is blank.</exception>
    public void MarkFailed(DateTime utcNow, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = DeliveryStatus.Failed;
        LastAttemptAt = utcNow;
        FailureReason = reason;
        AttemptCount++;
    }

    /// <summary>Records that no sender exists for the channel; does not count as an attempt and is never retried.</summary>
    /// <param name="utcNow">The current time, UTC.</param>
    public void MarkSkipped(DateTime utcNow)
    {
        Status = DeliveryStatus.Skipped;
        LastAttemptAt = utcNow;
    }
}
