namespace Shared.Infrastructure.Messaging.Dapper;

using Npgsql;

public interface INpgsqlConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default);
}
