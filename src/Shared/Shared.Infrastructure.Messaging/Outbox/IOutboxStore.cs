namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Storage seam for one publishing module's outbox. <see cref="AddAsync"/> is called by the bus
/// inside the module's unit of work; the other members are called by the module's
/// <see cref="OutboxWorker{TDbContext}"/> from its own scope. EF modules get
/// <c>EfOutboxStore&lt;TDbContext&gt;</c> via <c>AddOutbox&lt;TDbContext&gt;()</c>.
/// </summary>
public interface IOutboxStore
{
    /// <summary>Stages a message to be committed with the current unit of work.</summary>
    /// <param name="message">The serialised event and its metadata.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(OutboxMessage message, CancellationToken ct);

    /// <summary>Reads the oldest messages that have not been delivered yet.</summary>
    /// <param name="batchSize">Maximum number of messages to return.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>Pending messages ordered by <see cref="OutboxMessage.OccurredAt"/>.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct);

    /// <summary>Records a successful delivery so the message is never dispatched again.</summary>
    /// <param name="messageId">The <see cref="OutboxMessage.Id"/> that was delivered.</param>
    /// <param name="processedAt">Delivery time, in UTC.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct);

    /// <summary>Records a failed delivery attempt; the message stays pending for the next tick.</summary>
    /// <param name="messageId">The <see cref="OutboxMessage.Id"/> whose dispatch failed.</param>
    /// <param name="errorMessage">The failure message, kept for diagnostics.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task RecordFailureAsync(Guid messageId, string errorMessage, CancellationToken ct);
}
