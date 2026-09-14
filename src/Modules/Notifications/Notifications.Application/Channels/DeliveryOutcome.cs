namespace Notifications.Application.Channels;

/// <summary>
/// What a channel sender reports back for one send; the dispatcher maps it onto the delivery row.
/// </summary>
public enum DeliveryOutcome
{
    /// <summary>Delivered; the delivery is marked sent.</summary>
    Sent,
    /// <summary>Handed off without confirmation (WebSocket push awaiting ACK, or user offline); stays pending.</summary>
    Pending,
    /// <summary>Could not deliver; marked failed and retried later.</summary>
    Failed,
}
