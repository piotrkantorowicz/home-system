namespace Shared.Infrastructure.Messaging.Ef.Outbox;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class EfOutboxStore<TDbContext> : IOutboxStore, IOutboxDeadLetterStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfOutboxStore(TDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(OutboxMessage message, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .AddAsync(MapToEntity(message), ct).AsTask();

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, int maxAttempts, CancellationToken ct)
        => await _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.AttemptCount < maxAttempts)
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

    public async Task RecordFailureAsync(Guid messageId, string errorMessage, CancellationToken ct)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastError, errorMessage)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
    }

    public async Task<PagedList<OutboxDeadLetter>> ListAsync(
        int maxAttempts, int page, int pageSize, CancellationToken ct)
    {
        var dead = DeadLettered(maxAttempts);
        var total = await dead.CountAsync(ct);
        var items = await dead
            .OrderBy(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OutboxDeadLetter(
                x.Id, x.EventId, x.EventType, x.OccurredAt, x.AttemptCount, x.LastError))
            .ToListAsync(ct);

        return new PagedList<OutboxDeadLetter>(items, total, page, pageSize);
    }

    public async Task<OutboxBacklog> CountAsync(int maxAttempts, CancellationToken ct)
    {
        var failed = _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.AttemptCount > 0);

        var deadLettered = await failed.CountAsync(x => x.AttemptCount >= maxAttempts, ct);
        var retrying = await failed.CountAsync(x => x.AttemptCount < maxAttempts, ct);
        return new OutboxBacklog(deadLettered, retrying);
    }

    public async Task<bool> RequeueAsync(Guid messageId, CancellationToken ct)
    {
        var updated = await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId && x.ProcessedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AttemptCount, 0), ct);
        return updated > 0;
    }

    private IQueryable<OutboxMessageEntity> DeadLettered(int maxAttempts)
        => _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.AttemptCount >= maxAttempts);

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
