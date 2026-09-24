namespace Shared.Messaging.IntegrationTests.Fixtures;

using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// One PostgreSQL container shared by every messaging integration test through
/// <see cref="PostgresCollectionDefinition"/>. Each test class creates and drops its own schema in it.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    /// <summary>The Testcontainers instance, exposed for tests that need more than the connection string.</summary>
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("messaging_tests")
        .Build();

    /// <summary>Connection string of the running container.</summary>
    public string ConnectionString => Container.GetConnectionString();

    /// <summary>Starts the container; runs once per collection.</summary>
    public async ValueTask InitializeAsync() => await Container.StartAsync();

    /// <summary>Stops and removes the container.</summary>
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}

/// <summary>xUnit collection that shares one <see cref="PostgresContainerFixture"/> across the messaging integration tests.</summary>
[CollectionDefinition(nameof(PostgresCollectionDefinition))]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresContainerFixture> { }
