namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(DatabaseCollection.Name)]
public sealed class TestSupportEndpointsTests
{
    private static readonly IReadOnlyDictionary<string, string?> TestSupportEnabled
        = new Dictionary<string, string?> { ["E2ETestSupport:Enabled"] = "true" };

    private readonly DatabaseFixture _db;

    public TestSupportEndpointsTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task DELETE_PurgeMyData_WhenDisabled_Returns404()
    {
        // Factory with no E2ETestSupport override → endpoint must not be registered.
        using var factory = new DietPlannerWebApplicationFactory(_db.ConnectionString);
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/test-support/purge-my-data");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_PurgeMyData_WhenEnabled_Returns204()
    {
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: $"purge-user-{Guid.NewGuid():N}",
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/test-support/purge-my-data");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_PurgeMyData_WhenEnabled_RemovesOwnedRowsAcrossAllAggregates()
    {
        var userId = $"purge-user-{Guid.NewGuid():N}";
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: userId,
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        // Seed: one product, one meal entry dependency-free, one hydration config,
        // one goal, profile, notification prefs — via the regular public endpoints.
        var productResp = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest("Purge Chicken", 165m, 31m, 0m, 3.6m, 0m, "g", null, null));
        productResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var hydrationResp = await client.PutAsJsonAsync(
            "/api/v1/hydration/config",
            new UpdateHydrationConfigRequest(2500, 250, true));
        hydrationResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Sanity-check: rows exist before purge.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            (await db.Products.AnyAsync(x => x.CreatedByUserId == userId)).ShouldBeTrue();
            (await db.HydrationConfigs.AnyAsync(x => x.UserId == userId)).ShouldBeTrue();
        }

        // Act — purge.
        var purgeResp = await client.DeleteAsync("/api/v1/test-support/purge-my-data");
        purgeResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert — every DbSet has zero rows for this user.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            (await db.Products.AnyAsync(x => x.CreatedByUserId == userId)).ShouldBeFalse();
            (await db.Recipes.AnyAsync(x => x.CreatedByUserId == userId)).ShouldBeFalse();
            (await db.MealEntries.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
            (await db.UserGoals.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
            (await db.MealScheduleConfigs.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
            (await db.UserProfiles.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
            (await db.NotificationPreferences.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
            (await db.HydrationConfigs.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
            (await db.WaterIntakes.AnyAsync(x => x.UserId == userId)).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task DELETE_PurgeMyData_WhenUserHasNoData_Returns204()
    {
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: $"purge-empty-{Guid.NewGuid():N}",
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/test-support/purge-my-data");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
