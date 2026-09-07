namespace Notifications.Infrastructure.Persistence;

using Npgsql;
using Shared.Infrastructure.Messaging.Dapper;

internal sealed class NotificationsConnectionFactory : INpgsqlConnectionFactory, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public NotificationsConnectionFactory(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);

        // Opt out of System.Transactions auto-enlistment. The command dispatcher wraps every
        // command in an ambient TransactionScope; this module's DapperUnitOfWork owns its own
        // explicit transaction, and an enlisted connection cannot also begin one manually.
        builder.ConnectionStringBuilder.Enlist = false;

        _dataSource = builder.Build();
    }

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
        => await _dataSource.OpenConnectionAsync(ct);

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
