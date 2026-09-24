namespace DietPlanner.IntegrationTests.Infrastructure;

using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// One PostgreSQL container for the whole DietPlanner integration suite, schema created from the
/// EF model. Shared through <see cref="DatabaseCollectionDefinition"/> so tests run sequentially against it.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dietplanner_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>Connection string of the running container, handed to the application factory.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Starts the PostgreSQL container and prepares the schema; runs once per test collection.</summary>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<DietPlannerDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var db = new DietPlannerDbContext(options);
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>Stops and removes the container.</summary>
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
