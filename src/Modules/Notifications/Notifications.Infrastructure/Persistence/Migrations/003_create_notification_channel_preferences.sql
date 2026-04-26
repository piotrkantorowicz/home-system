CREATE TABLE notification_channel_preferences (
    id                uuid        PRIMARY KEY,
    user_id           text        NOT NULL UNIQUE,
    console_enabled   boolean     NOT NULL DEFAULT true,
    email_enabled     boolean     NOT NULL DEFAULT true,
    websocket_enabled boolean     NOT NULL DEFAULT true,
    updated_at        timestamptz NOT NULL
);
