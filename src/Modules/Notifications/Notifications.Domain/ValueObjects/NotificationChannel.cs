namespace Notifications.Domain.ValueObjects;

/// <summary>
/// A way of delivering a notification. Every enabled channel gets its own
/// <c>NotificationDelivery</c> row. Stored as text.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Written to the server log — the development fallback.</summary>
    Console,
    /// <summary>Sent by email (no sender implemented yet; deliveries are skipped).</summary>
    Email,
    /// <summary>Pushed over SignalR to the user's open browser sessions.</summary>
    WebSocket,
}
