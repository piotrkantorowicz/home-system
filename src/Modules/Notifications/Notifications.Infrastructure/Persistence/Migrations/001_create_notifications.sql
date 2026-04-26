CREATE TABLE notifications (
    id          uuid        PRIMARY KEY,
    user_id     text        NOT NULL,
    type        text        NOT NULL,
    title       text        NOT NULL,
    body        text        NOT NULL,
    payload     jsonb       NOT NULL,
    created_at  timestamptz NOT NULL,
    read_at     timestamptz NULL
);

CREATE INDEX ix_notifications_user_created
    ON notifications (user_id, created_at DESC);
