namespace Notifications.Domain.Abstractions;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken ct = default);
    Task MarkReadAsync(NotificationId id, DateTime readAt, CancellationToken ct = default);

    Task AddDeliveryAsync(NotificationDelivery delivery, CancellationToken ct = default);
    Task<NotificationDelivery?> GetDeliveryAsync(NotificationDeliveryId id, CancellationToken ct = default);
    Task UpdateDeliveryAsync(NotificationDelivery delivery, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDelivery>> GetFailedDeliveriesForRetryAsync(int batchSize, int maxAttempts, CancellationToken ct = default);
}
