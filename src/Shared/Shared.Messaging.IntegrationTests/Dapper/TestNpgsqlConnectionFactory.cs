namespace Shared.Messaging.IntegrationTests.Dapper;

using Npgsql;
using Shared.Infrastructure.Messaging.Dapper;

internal sealed class TestNpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly string _connectionString;

    public TestNpgsqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
