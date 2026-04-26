namespace Notifications.Infrastructure.Persistence.Repositories;

using Dapper;
using Notifications.Domain.Abstractions;
using Notifications.Infrastructure.Persistence.Sql;

internal sealed class InboxStore : IInboxStore
{
    private readonly NotificationsConnectionFactory _factory;

    public InboxStore(NotificationsConnectionFactory factory) => _factory = factory;

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
    {
        await using var connection = await _factory.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            InboxSql.Exists,
            new { EventId = eventId },
            cancellationToken: ct));
    }

    public async Task RecordAsync(Guid eventId, string eventType, DateTime consumedAt, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        await using var connection = await _factory.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            InboxSql.Insert,
            new { EventId = eventId, EventType = eventType, ConsumedAt = consumedAt },
            cancellationToken: ct));
    }
}
