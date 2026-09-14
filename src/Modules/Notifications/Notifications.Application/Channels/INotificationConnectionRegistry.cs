namespace Notifications.Application.Channels;

/// <summary>
/// In-memory count of open SignalR connections per user, so the WebSocket sender can leave a
/// delivery pending instead of pushing into the void when the user is offline.
/// </summary>
public interface INotificationConnectionRegistry
{
    /// <summary>Records one more open connection for the user.</summary>
    /// <param name="userId">Auth subject of the connected user.</param>
    void Track(string userId);
    /// <summary>Records one connection closed for the user.</summary>
    /// <param name="userId">Auth subject of the disconnected user.</param>
    void Untrack(string userId);
    /// <summary>Whether the user has at least one open connection.</summary>
    /// <param name="userId">Auth subject to check.</param>
    bool IsOnline(string userId);
}
