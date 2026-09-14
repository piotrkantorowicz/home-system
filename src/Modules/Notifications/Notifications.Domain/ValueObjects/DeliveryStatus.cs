namespace Notifications.Domain.ValueObjects;

/// <summary>
/// State of one channel delivery. <see cref="Failed"/> deliveries are retried by the retry worker
/// until the attempt limit; <see cref="Pending"/> WebSocket deliveries are replayed when the user
/// reconnects. Stored as text.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>Not delivered yet — never attempted, or pushed but not acknowledged.</summary>
    Pending,
    /// <summary>Delivered (for WebSocket: acknowledged by the client).</summary>
    Sent,
    /// <summary>The sender threw or returned failure; eligible for retry.</summary>
    Failed,
    /// <summary>No sender is registered for the channel; will not be retried.</summary>
    Skipped,
}
