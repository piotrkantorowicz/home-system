namespace Notifications.Infrastructure.Persistence.Sql;

internal static class InboxSql
{
    internal const string Exists = """
        SELECT EXISTS (SELECT 1 FROM inbox_messages WHERE event_id = @EventId);
        """;

    internal const string Insert = """
        INSERT INTO inbox_messages (event_id, event_type, consumed_at)
        VALUES (@EventId, @EventType, @ConsumedAt);
        """;
}
