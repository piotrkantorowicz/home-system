namespace Notifications.Application.Commands.BulkMarkNotificationsRead;

using Notifications.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class BulkMarkNotificationsReadCommandHandler(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<BulkMarkNotificationsReadCommand>
{
    public async Task HandleAsync(BulkMarkNotificationsReadCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserId);

        if (command.Ids.Count == 0) return;

        await repository.BulkMarkReadAsync(command.Ids, command.UserId, clock.GetUtcNow().UtcDateTime, ct);
        await unitOfWork.CommitAsync(ct);
    }
}
