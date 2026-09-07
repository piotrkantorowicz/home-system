namespace Household.IntegrationTests.Persistence;

using global::Household.Infrastructure.Persistence;
using Household.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

public sealed class HouseholdDbContextTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly HouseholdDatabaseFixture _fixture;

    public HouseholdDbContextTests(HouseholdDatabaseFixture fixture) => _fixture = fixture;

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
