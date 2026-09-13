namespace Shared.Messaging.IntegrationTests.Fixtures;

using Testcontainers.PostgreSql;
using Xunit;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("messaging_tests")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture> { }
