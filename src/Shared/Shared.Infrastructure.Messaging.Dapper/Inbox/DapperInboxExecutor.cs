namespace Shared.Infrastructure.Messaging.Dapper.Inbox;

using global::Dapper;
using Shared.Abstractions.Messaging;

internal sealed class DapperInboxExecutor<TFactory>(TFactory factory, TimeProvider clock) : IInboxExecutor
    where TFactory : INpgsqlConnectionFactory
{
    public async Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default)
    {
        await using var connection = await factory.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(InboxSql.Exists, new { EventId = eventId },
                transaction: transaction, cancellationToken: ct))
            .ConfigureAwait(false);

        if (exists)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        await handlerInvocation(ct).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(InboxSql.Insert,
                new { EventId = eventId, EventType = eventType, ConsumedAt = clock.GetUtcNow().UtcDateTime },
                transaction: transaction, cancellationToken: ct))
            .ConfigureAwait(false);

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
