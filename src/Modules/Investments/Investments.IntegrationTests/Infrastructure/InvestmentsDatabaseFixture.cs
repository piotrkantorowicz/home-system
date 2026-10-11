namespace Investments.IntegrationTests.Infrastructure;

using global::Investments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// One PostgreSQL container per test class, migrated once. Classes therefore run against
/// isolated databases and may execute in parallel.
/// </summary>
public sealed class InvestmentsDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _investments = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("investments_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>Connection string of the Investments database.</summary>
    public string ConnectionString => _investments.GetConnectionString();

    /// <summary>Starts the container and applies the module's EF migrations; runs once per test class.</summary>
    public async ValueTask InitializeAsync()
    {
        await _investments.StartAsync();

        await using var context = new InvestmentsDbContext(
            new DbContextOptionsBuilder<InvestmentsDbContext>().UseNpgsql(ConnectionString).Options);
        await context.Database.MigrateAsync();
    }

    /// <summary>Stops and removes the container.</summary>
    public async ValueTask DisposeAsync() => await _investments.DisposeAsync();
}
