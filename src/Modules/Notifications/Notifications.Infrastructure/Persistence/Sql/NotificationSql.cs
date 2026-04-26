namespace Notifications.Infrastructure.Persistence.Sql;

internal static class NotificationSql
{
    internal const string Insert = """
        INSERT INTO notifications
            (id, user_id, type, title, body, payload, created_at)
        VALUES
            (@Id, @UserId, @Type, @Title, @Body, @Payload::jsonb, @CreatedAt);
        """;

    internal const string SelectById = """
        SELECT id           AS Id,
               user_id      AS UserId,
               type         AS Type,
               title        AS Title,
               body         AS Body,
               payload      AS Payload,
               created_at   AS CreatedAt,
               read_at      AS ReadAt
        FROM notifications
        WHERE id = @Id;
        """;

    internal const string MarkRead = """
        UPDATE notifications
        SET read_at = @ReadAt
        WHERE id = @Id
          AND read_at IS NULL;
        """;
}
