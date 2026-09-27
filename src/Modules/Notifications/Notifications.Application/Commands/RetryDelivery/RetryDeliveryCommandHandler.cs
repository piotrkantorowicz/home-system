namespace Notifications.Application.Commands.RetryDelivery;

using Notifications.Domain.Abstractions;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class RetryDeliveryCommandHandler(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork)
    : ICommandHandler<RetryDeliveryCommand>
{
    public async Task HandleAsync(RetryDeliveryCommand command, CancellationToken ct = default)
    {
        var delivery = await repository.GetDeliveryAsync(NotificationDeliveryId.From(command.DeliveryId), ct)
            ?? throw new NotFoundException("NotificationDelivery", command.DeliveryId);

        delivery.Requeue();
        await repository.UpdateDeliveryAsync(delivery, ct);
        await unitOfWork.CommitAsync(ct);
    }
}
