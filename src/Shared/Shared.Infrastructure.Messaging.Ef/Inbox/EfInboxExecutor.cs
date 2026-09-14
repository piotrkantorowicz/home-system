namespace Shared.Infrastructure.Messaging.Ef.Inbox;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

internal sealed class EfInboxExecutor<TDbContext>(TDbContext dbContext, TimeProvider clock) : IInboxExecutor
    where TDbContext : DbContext
{
    public async Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var alreadyConsumed = await dbContext.Set<InboxMessageEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, ct)
            .ConfigureAwait(false);

        if (alreadyConsumed)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        await handlerInvocation(ct).ConfigureAwait(false);

        await dbContext.Set<InboxMessageEntity>()
            .AddAsync(new InboxMessageEntity
            {
                EventId = eventId,
                EventType = eventType,
                ConsumedAt = clock.GetUtcNow().UtcDateTime
            }, ct)
            .ConfigureAwait(false);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
