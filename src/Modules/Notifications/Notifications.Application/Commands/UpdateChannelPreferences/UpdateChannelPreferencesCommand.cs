namespace Notifications.Application.Commands.UpdateChannelPreferences;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Replaces the caller's channel switches, creating the row on first use.
/// </summary>
/// <param name="UserId">Auth subject of the caller; only their own rows are touched.</param>
/// <param name="ConsoleEnabled">Whether the console channel is on.</param>
/// <param name="EmailEnabled">Whether the email channel is on.</param>
/// <param name="WebSocketEnabled">Whether the WebSocket channel is on.</param>
public sealed record UpdateChannelPreferencesCommand(
    string UserId,
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled) : ICommand;
