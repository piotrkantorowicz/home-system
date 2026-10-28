namespace Assistant.IntegrationTests.Infrastructure;

using Testcontainers.PostgreSql;

/// <summary>
/// The databases behind the tools these tests drive: Investments (wallets) and Household (who the
/// caller is and what they may see), one container each per test class. <see cref="AssistantApiFactory"/>
/// migrates them through the modules' own migration entry points.
/// </summary>
public sealed class AssistantDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _investments = Build("investments_test");
    private readonly PostgreSqlContainer _household = Build("household_test");

    /// <summary>Connection string of the Investments database.</summary>
    public string InvestmentsConnectionString => _investments.GetConnectionString();

    /// <summary>Connection string of the Household database.</summary>
    public string HouseholdConnectionString => _household.GetConnectionString();

    /// <summary>Starts both containers.</summary>
    public async ValueTask InitializeAsync() => await Task.WhenAll(_investments.StartAsync(), _household.StartAsync());

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
