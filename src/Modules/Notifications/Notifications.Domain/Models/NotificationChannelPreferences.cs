namespace Notifications.Domain.Models;

using Notifications.Domain.ValueObjects;

/// <summary>
/// Which channels a user wants notifications on. One row per user, created with every channel
/// enabled the first time it is needed. A disabled channel gets no delivery row at all.
/// </summary>
public sealed class NotificationChannelPreferences
{
    private NotificationChannelPreferences() { }

    /// <summary>Creates the row with every channel enabled.</summary>
    /// <param name="id">Identifier for the new row.</param>
    /// <param name="userId">Auth subject of the user; required.</param>
    /// <param name="updatedAt">Creation time, UTC.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    public static NotificationChannelPreferences CreateDefault(
        NotificationChannelPreferencesId id,
        string userId,
        DateTime updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new NotificationChannelPreferences
        {
            Id = id,
            UserId = userId,
            ConsoleEnabled = true,
            EmailEnabled = true,
            WebSocketEnabled = true,
            UpdatedAt = updatedAt,
        };
    }

    /// <summary>Identifier.</summary>
    public NotificationChannelPreferencesId Id { get; private set; } = default!;
    /// <summary>Auth subject of the user.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>Whether the console channel is on.</summary>
    public bool ConsoleEnabled { get; private set; }
    /// <summary>Whether the email channel is on.</summary>
    public bool EmailEnabled { get; private set; }
    /// <summary>Whether the WebSocket channel is on.</summary>
    public bool WebSocketEnabled { get; private set; }
    /// <summary>Time of the last change, UTC.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Replaces all three switches at once.</summary>
    /// <param name="consoleEnabled">Whether the console channel is on.</param>
    /// <param name="emailEnabled">Whether the email channel is on.</param>
    /// <param name="webSocketEnabled">Whether the WebSocket channel is on.</param>
    /// <param name="updatedAt">The current time, UTC.</param>
    public void Update(
        bool consoleEnabled,
        bool emailEnabled,
        bool webSocketEnabled,
        DateTime updatedAt)
    {
        ConsoleEnabled = consoleEnabled;
        EmailEnabled = emailEnabled;
        WebSocketEnabled = webSocketEnabled;
        UpdatedAt = updatedAt;
    }

    /// <summary>Whether the given channel is on.</summary>
    /// <param name="channel">The channel to check.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="channel"/> is not a known value.</exception>
    public bool IsEnabled(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Console => ConsoleEnabled,
        NotificationChannel.Email => EmailEnabled,
        NotificationChannel.WebSocket => WebSocketEnabled,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };
}
