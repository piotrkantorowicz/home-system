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
    private readonly PostgreSqlContainer _dietPlanner = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine").WithDatabase("dp_hh_test").WithUsername("test").WithPassword("test").Build();

    private readonly PostgreSqlContainer _household = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine").WithDatabase("hh_test").WithUsername("test").WithPassword("test").Build();

    public string DietPlannerConnectionString => _dietPlanner.GetConnectionString();

    public string HouseholdConnectionString => _household.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dietPlanner.StartAsync(), _household.StartAsync());

        await using var dp = new DietPlannerDbContext(
            new DbContextOptionsBuilder<DietPlannerDbContext>().UseNpgsql(DietPlannerConnectionString).Options);
        await dp.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dietPlanner.DisposeAsync();
        await _household.DisposeAsync();
    }
}
