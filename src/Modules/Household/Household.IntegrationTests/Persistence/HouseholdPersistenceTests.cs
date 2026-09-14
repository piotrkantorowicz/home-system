namespace Household.IntegrationTests.Persistence;

using Household.Domain.ValueObjects;
using Household.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using HouseholdAggregate = Household.Domain.Aggregates.Household;
using HouseholdDb = Household.Infrastructure.Persistence.HouseholdDbContext;

/// <summary>Integration tests for <c>HouseholdPersistence</c> against a real PostgreSQL container.</summary>
public sealed class HouseholdPersistenceTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly HouseholdDatabaseFixture _fixture;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public HouseholdPersistenceTests(HouseholdDatabaseFixture fixture) => _fixture = fixture;

    private HouseholdDb NewContext()
        => new(new DbContextOptionsBuilder<HouseholdDb>().UseNpgsql(_fixture.ConnectionString).Options);

    /// <summary>With members, round trips: <c>Household</c> through ef configuration.</summary>
    [Fact]
    public async Task Household_WithMembers_RoundTrips_ThroughEfConfiguration()
    {
        var householdId = HouseholdId.New();
        var owner = PersonId.New();
        var child = PersonId.New();

        await using (var db = NewContext())
        {
            var household = HouseholdAggregate.Create(householdId, "Round Trip Home", owner);
            household.AddMember(child, HouseholdRole.Child, "Kiddo");
            db.Set<HouseholdAggregate>().Add(household);
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var reloaded = await db.Set<HouseholdAggregate>()
                .Include(h => h.Members)
                .SingleAsync(h => h.Id == householdId);

            reloaded.Name.ShouldBe("Round Trip Home");
            reloaded.Members.Count.ShouldBe(2);
            reloaded.RoleOf(owner).ShouldBe(HouseholdRole.Owner);
            reloaded.RoleOf(child).ShouldBe(HouseholdRole.Child);
            reloaded.Members.Single(m => m.PersonId == child).Nickname.ShouldBe("Kiddo");
        }
    }

    /// <summary><c>HouseholdMembers</c> are deleted and when the household is deleted.</summary>
    [Fact]
    public async Task HouseholdMembers_AreDeleted_WhenTheHouseholdIsDeleted()
    {
        var householdId = HouseholdId.New();

        await using (var db = NewContext())
        {
            db.Set<HouseholdAggregate>().Add(
                HouseholdAggregate.Create(householdId, "To Delete", PersonId.New()));
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var household = await db.Set<HouseholdAggregate>()
                .Include(h => h.Members)
                .SingleAsync(h => h.Id == householdId);
            db.Set<HouseholdAggregate>().Remove(household);
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext())
        {
            var orphanMembers = await db.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM household_members WHERE household_id = {householdId.Value}")
                .SingleAsync();
            orphanMembers.ShouldBe(0);
        }
    }
}
