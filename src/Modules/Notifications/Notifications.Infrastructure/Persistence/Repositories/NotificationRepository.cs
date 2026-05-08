namespace Notifications.Infrastructure.Persistence.Repositories;

using Dapper;
using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence.Sql;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly DapperUnitOfWork _uow;

    public NotificationRepository(DapperUnitOfWork uow) => _uow = uow;

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(new CommandDefinition(
            NotificationSql.Insert,
            new
            {
                Id = notification.Id.Value,
                notification.UserId,
                Type = notification.Type.ToString(),
                notification.Title,
                notification.Body,
                notification.Payload,
                notification.CreatedAt,
            },
            transaction: tx,
            cancellationToken: ct));
    }

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var connection = await _uow.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<NotificationRow>(new CommandDefinition(
            NotificationSql.SelectById,
            new { Id = id.Value },
            cancellationToken: ct));

        return row is null ? null : NotificationMapping.ToDomain(row);
    }

    public async Task MarkReadAsync(NotificationId id, DateTime readAt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(new CommandDefinition(
            NotificationSql.MarkRead,
            new { Id = id.Value, ReadAt = readAt },
            transaction: tx,
            cancellationToken: ct));
    }

    public async Task<int> BulkMarkReadAsync(
        IReadOnlyCollection<Guid> ids, string userId, DateTime readAt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (ids.Count == 0) return 0;

        var tx = await _uow.BeginTransactionAsync(ct);
        return await tx.Connection!.ExecuteAsync(new CommandDefinition(
            NotificationSql.BulkMarkReadByUser,
            new { Ids = ids.ToArray(), UserId = userId, ReadAt = readAt },
            transaction: tx,
            cancellationToken: ct));
    }

    public async Task AddDeliveryAsync(NotificationDelivery delivery, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(new CommandDefinition(
            NotificationDeliverySql.Insert,
            new
            {
                Id = delivery.Id.Value,
                NotificationId = delivery.NotificationId.Value,
                Channel = delivery.Channel.ToString(),
                Status = delivery.Status.ToString(),
                delivery.AttemptCount,
                delivery.LastAttemptAt,
                delivery.SentAt,
                delivery.FailureReason,
            },
            transaction: tx,
            cancellationToken: ct));
    }

    public async Task<NotificationDelivery?> GetDeliveryAsync(NotificationDeliveryId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var connection = await _uow.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<NotificationDeliveryRow>(new CommandDefinition(
            NotificationDeliverySql.SelectById,
            new { Id = id.Value },
            cancellationToken: ct));

        return row is null ? null : NotificationMapping.ToDomain(row);
    }

    public async Task UpdateDeliveryAsync(NotificationDelivery delivery, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(new CommandDefinition(
            NotificationDeliverySql.Update,
            new
            {
                Id = delivery.Id.Value,
                Status = delivery.Status.ToString(),
                delivery.AttemptCount,
                delivery.LastAttemptAt,
                delivery.SentAt,
                delivery.FailureReason,
            },
            transaction: tx,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<NotificationDelivery>> GetFailedDeliveriesForRetryAsync(
        int batchSize, int maxAttempts, CancellationToken ct = default)
    {
        var connection = await _uow.GetConnectionAsync(ct);
        var rows = await connection.QueryAsync<NotificationDeliveryRow>(new CommandDefinition(
            NotificationDeliverySql.SelectFailedForRetry,
            new { BatchSize = batchSize, MaxAttempts = maxAttempts },
            cancellationToken: ct));

        return rows.Select(NotificationMapping.ToDomain).ToList();
    }
}
