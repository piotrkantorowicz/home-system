namespace DietPlanner.IntegrationTests.Infrastructure;

using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// Stands up separate DietPlanner and Household databases so a test can exercise the
/// cross-module shopping-list aggregation (#223). The Household schema is migrated by the
/// host itself (via <c>MigrateHouseholdDatabaseAsync</c>) once a factory has booted.
/// </summary>
public sealed class HouseholdShoppingListFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dietPlanner = new PostgreSqlBuilder("postgres:17-alpine").WithDatabase("dp_hh_test").WithUsername("test").WithPassword("test").Build();

    private readonly PostgreSqlContainer _household = new PostgreSqlBuilder("postgres:17-alpine").WithDatabase("hh_test").WithUsername("test").WithPassword("test").Build();

    /// <summary>Connection string of the DietPlanner container.</summary>
    public string DietPlannerConnectionString => _dietPlanner.GetConnectionString();

    /// <summary>Connection string of the Household container.</summary>
    public string HouseholdConnectionString => _household.GetConnectionString();

    /// <summary>Starts both containers in parallel and creates the DietPlanner schema; Household is migrated by the host on first boot.</summary>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dietPlanner.StartAsync(), _household.StartAsync());

        await using var dp = new DietPlannerDbContext(
            new DbContextOptionsBuilder<DietPlannerDbContext>().UseNpgsql(DietPlannerConnectionString).Options);
        await dp.Database.EnsureCreatedAsync();
    }

    /// <summary>Stops and removes both containers.</summary>
    public async Task DisposeAsync()
    {
        await _dietPlanner.DisposeAsync();
        await _household.DisposeAsync();
    }
}
