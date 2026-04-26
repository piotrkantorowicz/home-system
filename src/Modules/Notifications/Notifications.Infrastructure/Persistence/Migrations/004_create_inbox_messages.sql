CREATE TABLE inbox_messages (
    event_id    uuid        PRIMARY KEY,
    event_type  text        NOT NULL,
    consumed_at timestamptz NOT NULL
);
