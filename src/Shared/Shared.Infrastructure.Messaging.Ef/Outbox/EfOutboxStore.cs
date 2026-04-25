namespace Shared.Infrastructure.Messaging.Ef.Outbox;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class EfOutboxStore<TDbContext> : IOutboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfOutboxStore(TDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(OutboxMessage message, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .AddAsync(MapToEntity(message), ct).AsTask();

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct)
        => await _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(batchSize)
            .Select(x => new OutboxMessage(
                x.Id, x.EventId, x.EventType, x.Payload, x.OccurredAt,
                x.ProcessedAt, x.AttemptCount, x.LastError))
            .ToListAsync(ct);

    public async Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedAt, processedAt), ct);
    }

    public async Task RecordFailureAsync(Guid messageId, string error, CancellationToken ct)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastError, error)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
    }

    private static OutboxMessageEntity MapToEntity(OutboxMessage m) => new()
    {
        Id = m.Id,
        EventId = m.EventId,
        EventType = m.EventType,
        Payload = m.Payload,
        OccurredAt = m.OccurredAt,
        ProcessedAt = m.ProcessedAt,
        AttemptCount = m.AttemptCount,
        LastError = m.LastError
    };
}
