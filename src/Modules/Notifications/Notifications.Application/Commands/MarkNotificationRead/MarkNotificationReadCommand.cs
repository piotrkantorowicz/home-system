namespace Notifications.Application.Commands.MarkNotificationRead;

using Shared.Abstractions.Cqrs;

public sealed record MarkNotificationReadCommand(Guid NotificationId, string UserId) : ICommand;
