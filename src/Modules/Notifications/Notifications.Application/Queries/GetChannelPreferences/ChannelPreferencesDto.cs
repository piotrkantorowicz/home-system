namespace Notifications.Application.Queries.GetChannelPreferences;

public sealed record ChannelPreferencesDto(
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled);
