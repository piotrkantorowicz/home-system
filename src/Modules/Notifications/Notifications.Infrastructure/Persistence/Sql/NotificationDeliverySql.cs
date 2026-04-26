namespace Notifications.Infrastructure.Persistence.Sql;

internal static class NotificationDeliverySql
{
    internal const string Insert = """
        INSERT INTO notification_deliveries
            (id, notification_id, channel, status, attempt_count, last_attempt_at, sent_at, failure_reason)
        VALUES
            (@Id, @NotificationId, @Channel, @Status, @AttemptCount, @LastAttemptAt, @SentAt, @FailureReason);
        """;

    internal const string SelectById = """
        SELECT id              AS Id,
               notification_id AS NotificationId,
               channel         AS Channel,
               status          AS Status,
               attempt_count   AS AttemptCount,
               last_attempt_at AS LastAttemptAt,
               sent_at         AS SentAt,
               failure_reason  AS FailureReason
        FROM notification_deliveries
        WHERE id = @Id;
        """;
}
