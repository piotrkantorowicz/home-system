CREATE TABLE notification_deliveries (
    id              uuid        PRIMARY KEY,
    notification_id uuid        NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    channel         text        NOT NULL,
    status          text        NOT NULL,
    attempt_count   int         NOT NULL DEFAULT 0,
    last_attempt_at timestamptz NULL,
    sent_at         timestamptz NULL,
    failure_reason  text        NULL
);

CREATE INDEX ix_notification_deliveries_failed_retry
    ON notification_deliveries (status, last_attempt_at)
    WHERE status = 'Failed';
