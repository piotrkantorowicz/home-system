namespace Shared.Infrastructure.Messaging.Outbox;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken ct);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct);
    Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct);
    Task RecordFailureAsync(Guid messageId, string error, CancellationToken ct);
}
