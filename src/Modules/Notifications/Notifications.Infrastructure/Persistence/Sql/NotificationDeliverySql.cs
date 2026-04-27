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

    internal const string Update = """
        UPDATE notification_deliveries
        SET status          = @Status,
            attempt_count   = @AttemptCount,
            last_attempt_at = @LastAttemptAt,
            sent_at         = @SentAt,
            failure_reason  = @FailureReason
        WHERE id = @Id;
        """;

    // Exponential backoff: gap in minutes = attempt_count^2 (1, 4, 9, 16, 25)
    internal const string SelectFailedForRetry = """
        SELECT id              AS Id,
               notification_id AS NotificationId,
               channel         AS Channel,
               status          AS Status,
               attempt_count   AS AttemptCount,
               last_attempt_at AS LastAttemptAt,
               sent_at         AS SentAt,
               failure_reason  AS FailureReason
        FROM notification_deliveries
        WHERE status = 'Failed'
          AND attempt_count < @MaxAttempts
          AND (last_attempt_at IS NULL
               OR last_attempt_at < now() - ((attempt_count * attempt_count)::text || ' minutes')::interval)
        ORDER BY last_attempt_at NULLS FIRST
        LIMIT @BatchSize;
        """;
}
