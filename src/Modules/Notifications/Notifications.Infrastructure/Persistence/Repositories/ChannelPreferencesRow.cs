namespace Notifications.Infrastructure.Persistence.Repositories;

internal sealed record ChannelPreferencesRow(
    Guid Id,
    string UserId,
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled,
    DateTime UpdatedAt);
