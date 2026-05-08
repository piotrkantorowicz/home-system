namespace Notifications.Application.Commands.BulkMarkNotificationsRead;

using Shared.Abstractions.Cqrs;

public sealed record BulkMarkNotificationsReadCommand(
    IReadOnlyCollection<Guid> Ids,
    string UserId) : ICommand;
