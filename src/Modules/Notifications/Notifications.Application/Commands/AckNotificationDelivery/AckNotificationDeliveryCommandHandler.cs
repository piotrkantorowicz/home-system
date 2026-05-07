namespace Notifications.Application.Commands.AckNotificationDelivery;

using Notifications.Domain.Abstractions;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class AckNotificationDeliveryCommandHandler(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork)
    : ICommandHandler<AckNotificationDeliveryCommand>
{
    public async Task HandleAsync(AckNotificationDeliveryCommand command, CancellationToken ct = default)
    {
        var deliveryId = NotificationDeliveryId.From(command.DeliveryId);
        var delivery = await repository.GetDeliveryAsync(deliveryId, ct)
            ?? throw new NotFoundException("NotificationDelivery", command.DeliveryId);

        var notification = await repository.GetByIdAsync(delivery.NotificationId, ct)
            ?? throw new NotFoundException("NotificationDelivery", command.DeliveryId);

        if (notification.UserId != command.UserId)
            throw new NotFoundException("NotificationDelivery", command.DeliveryId);

        if (delivery.Status == DeliveryStatus.Sent)
            return;

        delivery.MarkSent(DateTime.UtcNow);
        await repository.UpdateDeliveryAsync(delivery, ct);
        await unitOfWork.CommitAsync(ct);
    }
}
