namespace Notifications.Domain.Models;

using Notifications.Domain.ValueObjects;

public sealed class NotificationChannelPreferences
{
    private NotificationChannelPreferences() { }

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

    public NotificationChannelPreferencesId Id { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public bool ConsoleEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool WebSocketEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

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

    public bool IsEnabled(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Console => ConsoleEnabled,
        NotificationChannel.Email => EmailEnabled,
        NotificationChannel.WebSocket => WebSocketEnabled,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };
}
