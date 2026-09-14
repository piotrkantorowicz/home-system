namespace Shared.Infrastructure.Messaging.Dapper;

using Npgsql;

/// <summary>
/// Opens connections to one Style-2 module's PostgreSQL database. Each module implements it over its
/// own <c>NpgsqlDataSource</c>; the Dapper inbox executor is generic over the factory type so the
/// inbox row lands in the consuming module's database, never in a shared pool.
/// </summary>
public interface INpgsqlConnectionFactory
{
    /// <summary>Opens a new pooled connection; the caller owns and disposes it.</summary>
    /// <param name="ct">Propagates cancellation to the connection open.</param>
    /// <returns>An open connection to the module's database.</returns>
    Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default);
}
