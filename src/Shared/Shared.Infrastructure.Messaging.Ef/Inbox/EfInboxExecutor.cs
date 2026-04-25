namespace Shared.Infrastructure.Messaging.Ef.Inbox;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

internal sealed class EfInboxExecutor<TDbContext> : IInboxExecutor
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfInboxExecutor(TDbContext dbContext) => _dbContext = dbContext;

    public async Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var alreadyConsumed = await _dbContext.Set<InboxMessageEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, ct)
            .ConfigureAwait(false);

        if (alreadyConsumed)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        await handlerInvocation(ct).ConfigureAwait(false);

        await _dbContext.Set<InboxMessageEntity>()
            .AddAsync(new InboxMessageEntity
            {
                EventId = eventId,
                EventType = eventType,
                ConsumedAt = DateTime.UtcNow
            }, ct)
            .ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
