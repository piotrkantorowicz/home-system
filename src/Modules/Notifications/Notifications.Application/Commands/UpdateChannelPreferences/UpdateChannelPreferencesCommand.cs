namespace Notifications.Application.Commands.UpdateChannelPreferences;

using Shared.Abstractions.Cqrs;

public sealed record UpdateChannelPreferencesCommand(
    string UserId,
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled) : ICommand;
