namespace Notifications.Domain.Abstractions;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

/// <summary>
/// Dapper-backed access to <see cref="Notification"/> and <see cref="NotificationDelivery"/> rows.
/// Writes execute immediately on the unit of work's transaction and become durable on
/// <c>CommitAsync</c>. List queries for the API bypass this and use Dapper directly.
/// </summary>
public interface INotificationRepository
{
    /// <summary>Inserts a notification.</summary>
    /// <param name="notification">The notification to insert.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(Notification notification, CancellationToken ct = default);
    /// <summary>Loads a notification.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The notification, or <see langword="null"/> when it does not exist.</returns>
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken ct = default);
    /// <summary>Sets the read time of one notification if it is still unread.</summary>
    /// <param name="id">The notification.</param>
    /// <param name="readAt">The read time, UTC.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task MarkReadAsync(NotificationId id, DateTime readAt, CancellationToken ct = default);
    /// <summary>Sets the read time of every listed notification the user owns that is still unread.</summary>
    /// <param name="ids">The notifications to mark.</param>
    /// <param name="userId">Auth subject of the owner; rows of other users are ignored.</param>
    /// <param name="readAt">The read time, UTC.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>How many rows changed.</returns>
    Task<int> BulkMarkReadAsync(IReadOnlyCollection<Guid> ids, string userId, DateTime readAt, CancellationToken ct = default);

    /// <summary>Inserts a delivery row.</summary>
    /// <param name="delivery">The delivery to insert.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddDeliveryAsync(NotificationDelivery delivery, CancellationToken ct = default);
    /// <summary>Loads a delivery.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The delivery, or <see langword="null"/> when it does not exist.</returns>
    Task<NotificationDelivery?> GetDeliveryAsync(NotificationDeliveryId id, CancellationToken ct = default);
    /// <summary>Writes a delivery's status, attempt count and timestamps.</summary>
    /// <param name="delivery">The delivery to update.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task UpdateDeliveryAsync(NotificationDelivery delivery, CancellationToken ct = default);
    /// <summary>Reads failed deliveries whose attempt count is below the limit, oldest attempt first, for the retry worker.</summary>
    /// <param name="batchSize">Maximum rows to return.</param>
    /// <param name="maxAttempts">Deliveries with this many attempts or more are excluded.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<IReadOnlyList<NotificationDelivery>> GetFailedDeliveriesForRetryAsync(int batchSize, int maxAttempts, CancellationToken ct = default);
}
