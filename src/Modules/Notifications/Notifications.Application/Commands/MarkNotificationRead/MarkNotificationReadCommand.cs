namespace Notifications.Application.Commands.MarkNotificationRead;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks one of the caller's notifications read; a no-op if already read.
/// </summary>
/// <param name="NotificationId">The notification; must belong to the caller.</param>
/// <param name="UserId">Auth subject of the caller; only their own rows are touched.</param>
public sealed record MarkNotificationReadCommand(Guid NotificationId, string UserId) : ICommand;
