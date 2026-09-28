namespace Notifications.Infrastructure.Commands;

using Microsoft.Extensions.Options;
using Notifications.Application.Commands.RetryAllDeliveries;
using Notifications.Domain.Abstractions;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Workers;
using Shared.Abstractions.Cqrs;

// Lives in Infrastructure (like the admin query handlers) because "dead-lettered" is defined by
// RetryDeliveryWorkerOptions.MaxAttempts, which the Application layer can't see.
internal sealed class RetryAllDeliveriesCommandHandler(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork,
    IOptions<RetryDeliveryWorkerOptions> options)
    : ICommandHandler<RetryAllDeliveriesCommand, RetryAllDeliveriesResultDto>
{
    public async Task<RetryAllDeliveriesResultDto> HandleAsync(
        RetryAllDeliveriesCommand command, CancellationToken ct = default)
    {
        var dead = await repository.GetDeadLetteredDeliveriesAsync(
            RetryAllDeliveriesCommand.MaxBatch, options.Value.MaxAttempts, ct);

        foreach (var delivery in dead)
        {
            var retry = delivery.Retry(NotificationDeliveryId.New());
            await repository.UpdateDeliveryAsync(delivery, ct);
            await repository.AddDeliveryAsync(retry, ct);
        }

        await unitOfWork.CommitAsync(ct);
        return new RetryAllDeliveriesResultDto(dead.Count);
    }
}
