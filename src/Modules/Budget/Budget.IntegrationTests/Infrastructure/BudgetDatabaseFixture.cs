namespace Budget.IntegrationTests.Infrastructure;

using global::Budget.Infrastructure.Persistence;
using global::Household.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// Two PostgreSQL containers per test class — one per module database, since Budget resolves
/// the caller's household through Household Contracts — each migrated once. Classes therefore
/// run against isolated databases and may execute in parallel.
/// </summary>
public sealed class BudgetDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _budget = Build("budget_test");
    private readonly PostgreSqlContainer _household = Build("household_test");

    /// <summary>Connection string of the Budget database.</summary>
    public string BudgetConnectionString => _budget.GetConnectionString();

    /// <summary>Connection string of the Household database.</summary>
    public string HouseholdConnectionString => _household.GetConnectionString();

    /// <summary>Starts both containers and applies both modules' EF migrations; runs once per test class.</summary>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_budget.StartAsync(), _household.StartAsync());

        await using var budget = new BudgetDbContext(
            new DbContextOptionsBuilder<BudgetDbContext>().UseNpgsql(BudgetConnectionString).Options);
        await budget.Database.MigrateAsync();

        await using var household = new HouseholdDbContext(
            new DbContextOptionsBuilder<HouseholdDbContext>().UseNpgsql(HouseholdConnectionString).Options);
        await household.Database.MigrateAsync();
    }

    /// <summary>Stops and removes both containers.</summary>
    public async ValueTask DisposeAsync()
    {
        await _budget.DisposeAsync();
        await _household.DisposeAsync();
    }

    private static PostgreSqlContainer Build(string database)
        => new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase(database)
            .WithUsername("test")
            .WithPassword("test")
            .Build();
}
