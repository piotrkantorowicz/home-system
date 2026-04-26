namespace Notifications.Infrastructure.Persistence.Sql;

internal static class ChannelPreferencesSql
{
    internal const string Insert = """
        INSERT INTO notification_channel_preferences
            (id, user_id, console_enabled, email_enabled, websocket_enabled, updated_at)
        VALUES
            (@Id, @UserId, @ConsoleEnabled, @EmailEnabled, @WebSocketEnabled, @UpdatedAt);
        """;

    internal const string Update = """
        UPDATE notification_channel_preferences
        SET console_enabled   = @ConsoleEnabled,
            email_enabled     = @EmailEnabled,
            websocket_enabled = @WebSocketEnabled,
            updated_at        = @UpdatedAt
        WHERE id = @Id;
        """;

    internal const string SelectByUserId = """
        SELECT id                AS Id,
               user_id           AS UserId,
               console_enabled   AS ConsoleEnabled,
               email_enabled     AS EmailEnabled,
               websocket_enabled AS WebSocketEnabled,
               updated_at        AS UpdatedAt
        FROM notification_channel_preferences
        WHERE user_id = @UserId;
        """;
}
