namespace Notifications.Application.Queries.GetChannelPreferences;

/// <summary>
/// A user's channel switches.
/// </summary>
/// <param name="ConsoleEnabled">Whether the console channel is on.</param>
/// <param name="EmailEnabled">Whether the email channel is on.</param>
/// <param name="WebSocketEnabled">Whether the WebSocket channel is on.</param>
public sealed record ChannelPreferencesDto(
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled);
