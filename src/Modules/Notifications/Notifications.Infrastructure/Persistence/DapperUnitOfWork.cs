namespace Notifications.Infrastructure.Persistence;

using Npgsql;
using Shared.Abstractions.Core.Domain;

internal sealed class DapperUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly NotificationsConnectionFactory _factory;
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public DapperUnitOfWork(NotificationsConnectionFactory factory)
        => _factory = factory;

    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is not null) return _connection;
        _connection = await _factory.OpenAsync(ct);
        return _connection;
    }

    public async Task<NpgsqlTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        _transaction ??= await connection.BeginTransactionAsync(ct);
        return _transaction;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null) await _transaction.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
