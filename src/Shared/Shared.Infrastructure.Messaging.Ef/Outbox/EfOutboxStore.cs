namespace Shared.Infrastructure.Messaging.Ef.Outbox;

using System.Transactions;
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

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(
        int batchSize, int maxAttempts, DateTime now, CancellationToken ct)
        => await _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.RetriedAt == null && x.AttemptCount < maxAttempts
                && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
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

    public async Task RecordFailureAsync(Guid messageId, string errorMessage, DateTime nextAttemptAt, CancellationToken ct)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastError, errorMessage)
                .SetProperty(x => x.NextAttemptAt, nextAttemptAt)
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
                x.Id, x.EventId, x.EventType, x.OccurredAt, x.AttemptCount, x.LastError, x.RetryOf))
            .ToListAsync(ct);

        return new PagedList<OutboxDeadLetter>(items, total, page, pageSize);
    }

    public async Task<OutboxBacklog> CountAsync(int maxAttempts, CancellationToken ct)
    {
        var failed = _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.RetriedAt == null && x.AttemptCount > 0);

        var deadLettered = await failed.CountAsync(x => x.AttemptCount >= maxAttempts, ct);
        var retrying = await failed.CountAsync(x => x.AttemptCount < maxAttempts, ct);
        return new OutboxBacklog(deadLettered, retrying);
    }

    public async Task<bool> RetryAsync(Guid messageId, DateTime now, CancellationToken ct)
        => await RetryEachAsync(
            _dbContext.Set<OutboxMessageEntity>().AsNoTracking()
                .Where(x => x.Id == messageId && x.ProcessedAt == null && x.RetriedAt == null),
            now, ct) == 1;

    public Task<int> RetryAllAsync(int maxAttempts, DateTime now, int limit, CancellationToken ct)
        => RetryEachAsync(DeadLettered(maxAttempts).OrderBy(x => x.OccurredAt).Take(limit), now, ct);

    public Task<OutboxPayload?> GetPayloadAsync(Guid messageId, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.Id == messageId)
            .Select(x => new OutboxPayload(x.Id, x.EventType, x.Payload))
            .FirstOrDefaultAsync(ct);

    // Claims each candidate with a conditional UPDATE and stages a replacement (same event, fresh
    // attempts) only for the rows this call claimed; a concurrent retry that got there first leaves
    // 0 rows affected, so every dead letter gets one replacement. One transaction: it joins the
    // dispatcher's ambient scope, or is its own when called outside a command.
    // ponytail: one UPDATE per row (<= the 500-row retry-all cap); batch the claim if that grows.
    private async Task<int> RetryEachAsync(
        IQueryable<OutboxMessageEntity> candidates, DateTime now, CancellationToken ct)
    {
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        var set = _dbContext.Set<OutboxMessageEntity>();
        var retried = 0;
        foreach (var original in await candidates.ToListAsync(ct))
        {
            var claimed = await set
                .Where(x => x.Id == original.Id && x.ProcessedAt == null && x.RetriedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RetriedAt, now), ct);
            if (claimed == 0) continue;

            set.Add(new OutboxMessageEntity
            {
                Id = Guid.CreateVersion7(),
                EventId = original.EventId,
                EventType = original.EventType,
                Payload = original.Payload,
                OccurredAt = original.OccurredAt,
                RetryOf = original.Id,
            });
            retried++;
        }

        await _dbContext.SaveChangesAsync(ct);
        scope.Complete();
        return retried;
    }

    private IQueryable<OutboxMessageEntity> DeadLettered(int maxAttempts)
        => _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && x.RetriedAt == null && x.AttemptCount >= maxAttempts);

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
