namespace Household.IntegrationTests.Infrastructure;

using global::Household.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// A PostgreSQL container per test class: every Household integration test class declares
/// <c>IClassFixture&lt;HouseholdDatabaseFixture&gt;</c>, so xUnit creates one fixture — one container,
/// migrated once — per class and shares it only among that class's tests. Classes therefore run
/// against isolated databases and may execute in parallel; each boots its own
/// <see cref="HouseholdApiFactory"/> against its fixture.
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

    /// <summary>Starts the PostgreSQL container and applies the EF migrations; runs once per test class.</summary>
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
