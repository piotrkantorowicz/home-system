namespace Notifications.Infrastructure.Persistence;

using Npgsql;
using Shared.Infrastructure.Messaging.Dapper;

internal sealed class NotificationsConnectionFactory : INpgsqlConnectionFactory, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public NotificationsConnectionFactory(string connectionString)
        => _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
        => await _dataSource.OpenConnectionAsync(ct);

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
