namespace Investments.IntegrationTests.Infrastructure;

using global::Household.Infrastructure.Persistence;
using global::Investments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// Two PostgreSQL containers per test class — one per module database, since Investments resolves
/// the caller's household through Household Contracts — each migrated once. Classes therefore run
/// against isolated databases and may execute in parallel.
/// </summary>
public sealed class InvestmentsDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _investments = Build("investments_test");
    private readonly PostgreSqlContainer _household = Build("household_test");

    /// <summary>Connection string of the Investments database.</summary>
    public string ConnectionString => _investments.GetConnectionString();

    /// <summary>Connection string of the Household database.</summary>
    public string HouseholdConnectionString => _household.GetConnectionString();

    /// <summary>Starts both containers and applies both modules' EF migrations; runs once per test class.</summary>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_investments.StartAsync(), _household.StartAsync());

        await using var investments = new InvestmentsDbContext(
            new DbContextOptionsBuilder<InvestmentsDbContext>().UseNpgsql(ConnectionString).Options);
        await investments.Database.MigrateAsync();

        await using var household = new HouseholdDbContext(
            new DbContextOptionsBuilder<HouseholdDbContext>().UseNpgsql(HouseholdConnectionString).Options);
        await household.Database.MigrateAsync();
    }

    /// <summary>Stops and removes both containers.</summary>
    public async ValueTask DisposeAsync()
    {
        await _investments.DisposeAsync();
        await _household.DisposeAsync();
    }

    private static PostgreSqlContainer Build(string database)
        => new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase(database)
            .WithUsername("test")
            .WithPassword("test")
            .Build();
}
