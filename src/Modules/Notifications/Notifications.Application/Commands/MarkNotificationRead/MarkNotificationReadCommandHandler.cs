namespace Notifications.Application.Commands.MarkNotificationRead;

using Notifications.Domain.Abstractions;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class MarkNotificationReadCommandHandler(
    INotificationRepository repository)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task HandleAsync(MarkNotificationReadCommand command, CancellationToken ct = default)
    {
        var id = NotificationId.From(command.NotificationId);
        var notification = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Notification", command.NotificationId);

        if (notification.UserId != command.UserId)
            throw new NotFoundException("Notification", command.NotificationId);

        await repository.MarkReadAsync(id, DateTime.UtcNow, ct);
    }
}
