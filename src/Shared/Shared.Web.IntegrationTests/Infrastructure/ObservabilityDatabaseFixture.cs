namespace Shared.Web.IntegrationTests.Infrastructure;

using Testcontainers.PostgreSql;

/// <summary>A PostgreSQL container the observability tests query, so Npgsql emits a real DB span.</summary>
public sealed class ObservabilityDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("observability_tests")
        .Build();

    /// <summary>Connection string of the running container.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await _container.StartAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
