namespace Notifications.Infrastructure.Persistence;

using Npgsql;
using Shared.Infrastructure.Messaging.Dapper;

internal sealed class NotificationsConnectionFactory : INpgsqlConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NotificationsConnectionFactory(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
        => await _dataSource.OpenConnectionAsync(ct);
}
