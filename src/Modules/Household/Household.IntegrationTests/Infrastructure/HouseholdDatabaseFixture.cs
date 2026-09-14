namespace Household.IntegrationTests.Infrastructure;

using global::Household.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// One PostgreSQL container for the whole Household integration suite, migrated once. Test classes
/// take it through <c>IClassFixture</c> and each boots its own <see cref="HouseholdApiFactory"/>.
/// </summary>
public sealed class HouseholdDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("household_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>Connection string of the running container, handed to the application factory.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Starts the PostgreSQL container and prepares the schema; runs once per test collection.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<HouseholdDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var db = new HouseholdDbContext(options);
        await db.Database.MigrateAsync();
    }

    /// <summary>Stops and removes the container.</summary>
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
