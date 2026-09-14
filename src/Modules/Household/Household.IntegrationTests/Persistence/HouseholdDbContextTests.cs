namespace Household.IntegrationTests.Persistence;

using global::Household.Infrastructure.Persistence;
using Household.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

/// <summary>Integration tests for <c>HouseholdDbContext</c> against a real PostgreSQL container.</summary>
public sealed class HouseholdDbContextTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly HouseholdDatabaseFixture _fixture;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public HouseholdDbContextTests(HouseholdDatabaseFixture fixture) => _fixture = fixture;

    /// <summary>Apply: <c>Migrations</c> create the outbox and inbox tables.</summary>
    [Fact]
    public async Task Migrations_Apply_AndCreateTheOutboxAndInboxTables()
    {
        var options = new DbContextOptionsBuilder<HouseholdDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        await using var db = new HouseholdDbContext(options);

        var applied = await db.Database.GetAppliedMigrationsAsync();
        applied.ShouldContain(m => m.EndsWith("InitialCreate"));

        var tables = await db.Database
            .SqlQuery<string>($"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();

        tables.ShouldContain("outbox_messages");
        tables.ShouldContain("inbox_messages");
    }
}
