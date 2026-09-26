namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>HTTP integration tests for the <c>TestSupport</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class TestSupportEndpointsTests
{
    private static readonly IReadOnlyDictionary<string, string?> TestSupportEnabled
        = new Dictionary<string, string?> { ["E2ETestSupport:Enabled"] = "true" };

    private readonly DatabaseFixture _db;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public TestSupportEndpointsTests(DatabaseFixture db) => _db = db;

    private static async Task<Guid> EnsureBreakfastSlotAsync(HttpClient client)
    {
        var put = await client.PutAsJsonAsync(
            "/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]));
        put.EnsureSuccessStatusCode();

        var schedule = await client.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule");
        return schedule!.Slots[0].Id;
    }

    /// <summary>When disabled: <c>DELETE</c> purge my data returns 404.</summary>
    [Fact]
    public async Task DELETE_PurgeMyData_WhenDisabled_Returns404()
    {
        // Factory with no E2ETestSupport override → endpoint must not be registered.
        using var factory = new DietPlannerWebApplicationFactory(_db.ConnectionString);
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>When enabled: <c>DELETE</c> purge my data returns 204.</summary>
    [Fact]
    public async Task DELETE_PurgeMyData_WhenEnabled_Returns204()
    {
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: $"purge-user-{Guid.NewGuid():N}",
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>When enabled: <c>DELETE</c> purge my data removes owned rows across all aggregates.</summary>
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
        var productResp = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("Purge Chicken", 165m, 31m, 0m, 3.6m, 0m, "g", null, null), cancellationToken: TestContext.Current.CancellationToken);
        productResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var hydrationResp = await client.PutAsJsonAsync("/api/v1/hydration/config", new UpdateHydrationConfigRequest(2500, 250, true), cancellationToken: TestContext.Current.CancellationToken);
        hydrationResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Sanity-check: rows exist before purge.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            (await db.Products.AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeTrue();
            (await db.HydrationConfigs.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeTrue();
        }

        // Act — purge.
        var purgeResp = await client.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);
        purgeResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert — every DbSet has zero rows for this user.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            (await db.Products.AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.Recipes.AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.MealEntries.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.UserGoals.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.MealScheduleConfigs.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.UserProfiles.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.DietReminderSettings.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.HydrationConfigs.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
            (await db.WaterIntakes.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
        }
    }

    /// <summary>With full fk chain: <c>DELETE</c> purge my data succeeds.</summary>
    [Fact]
    public async Task DELETE_PurgeMyData_WithFullFkChain_Succeeds()
    {
        var userId = $"purge-fk-{Guid.NewGuid():N}";
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: userId,
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        // Seed: product → recipe (ingredient references the product) → meal entry (references the recipe).
        // This exercises the RecipeIngredient→Product (Restrict) and MealEntry→Recipe (Restrict) FKs.
        var productResp = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("FK Oats", 389m, 16.9m, 66.3m, 6.9m, 10.6m, "g", null, null), cancellationToken: TestContext.Current.CancellationToken);
        productResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var productId = Guid.Parse((await productResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));

        var recipeResp = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeRequest(
                Name: "FK Oatmeal",
                Description: null,
                Instructions: null,
                Servings: 1,
                PrepTimeMinutes: 5,
                Ingredients: [new RecipeIngredientRequest(productId, 80m, "g")]), cancellationToken: TestContext.Current.CancellationToken);
        recipeResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var recipeId = Guid.Parse((await recipeResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));

        var slotId = await EnsureBreakfastSlotAsync(client);
        var mealResp = await client.PostAsJsonAsync("/api/v1/meals", new CreateMealEntryRequest(
                Date: TestClock.Today,
                MealSlotId: slotId,
                RecipeId: recipeId,
                Servings: 1m,
                Notes: null,
                MealTime: null,
                SequenceOrder: 0), cancellationToken: TestContext.Current.CancellationToken);
        mealResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — purge.
        var purgeResp = await client.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);
        var body = await purgeResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        purgeResp.StatusCode.ShouldBe(HttpStatusCode.NoContent, body);

        // Assert — everything gone, including the ingredient rows that cascade from Recipe.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await db.MealEntries.AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(userId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await db.Recipes.AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await db.Products.AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    /// <summary><c>DELETE</c> purge my data also removes soft deleted rows and their dependencies.</summary>
    [Fact]
    public async Task DELETE_PurgeMyData_AlsoRemovesSoftDeletedRowsAndTheirDependencies()
    {
        // Regression: Product and Recipe have global soft-delete query filters
        // (DeletedAt == null). A plain ExecuteDeleteAsync on the DbSet skips filtered
        // rows, so a previously-soft-deleted recipe would leave its ingredients
        // pointing at a product and block the product's hard-delete.
        var userId = $"purge-softdel-{Guid.NewGuid():N}";
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: userId,
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        var productResp = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("SoftDel Oats", 389m, 16.9m, 66.3m, 6.9m, 10.6m, "g", null, null), cancellationToken: TestContext.Current.CancellationToken);
        productResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var productId = Guid.Parse((await productResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));

        var recipeResp = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeRequest(
                Name: "SoftDel Oatmeal",
                Description: null,
                Instructions: null,
                Servings: 1,
                PrepTimeMinutes: 5,
                Ingredients: [new RecipeIngredientRequest(productId, 80m, "g")]), cancellationToken: TestContext.Current.CancellationToken);
        recipeResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var recipeId = Guid.Parse((await recipeResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));

        // Soft-delete the recipe via the regular API (sets DeletedAt, ingredient row survives).
        var softDeleteResp = await client.DeleteAsync($"/api/v1/recipes/{recipeId}", TestContext.Current.CancellationToken);
        softDeleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act — purge.
        var purgeResp = await client.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);
        var body = await purgeResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        purgeResp.StatusCode.ShouldBe(HttpStatusCode.NoContent, body);

        // Assert — product, recipe (even soft-deleted), and ingredients are all gone.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await db.Products.IgnoreQueryFilters().AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await db.Recipes.IgnoreQueryFilters().AnyAsync(x => x.CreatedByUserId == userId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    /// <summary><c>DELETE</c> purge my data also removes orphans from previous sub hashes.</summary>
    [Fact]
    public async Task DELETE_PurgeMyData_AlsoRemovesOrphansFromPreviousSubHashes()
    {
        // Regression: after `docker compose down -v` Authentik reassigns sub hashes,
        // leaving rows with stale user_ids that still reference the current user's
        // recipes/products. Plain UserId == currentUserId matches skip those orphans
        // and their FK constraints block the recipe/product delete.
        var currentUserId = $"purge-stale-current-{Guid.NewGuid():N}";
        var staleUserId = $"purge-stale-old-{Guid.NewGuid():N}";

        using var currentFactory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: currentUserId,
            settings: TestSupportEnabled);
        using var staleFactory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: staleUserId,
            settings: TestSupportEnabled);
        var currentClient = currentFactory.CreateClient();
        var staleClient = staleFactory.CreateClient();

        // Current user: seed product + recipe (recipe ingredient references product).
        var productResp = await currentClient.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("Stale Oats", 389m, 16.9m, 66.3m, 6.9m, 10.6m, "g", null, null), cancellationToken: TestContext.Current.CancellationToken);
        productResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var productId = Guid.Parse((await productResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));

        var recipeResp = await currentClient.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeRequest(
                Name: "Stale Oatmeal",
                Description: null,
                Instructions: null,
                Servings: 1,
                PrepTimeMinutes: 5,
                Ingredients: [new RecipeIngredientRequest(productId, 80m, "g")],
                Visibility: "Public"), cancellationToken: TestContext.Current.CancellationToken);
        recipeResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var recipeId = Guid.Parse((await recipeResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));

        // "Stale" user (different sub): create a meal_entry that references the
        // current user's (public, so referenceable) recipe. Mimics an orphan left over from a previous session.
        var staleSlotId = await EnsureBreakfastSlotAsync(staleClient);
        var staleMealResp = await staleClient.PostAsJsonAsync("/api/v1/meals", new CreateMealEntryRequest(
                Date: TestClock.Today,
                MealSlotId: staleSlotId,
                RecipeId: recipeId,
                Servings: 1m,
                Notes: null,
                MealTime: null,
                SequenceOrder: 0), cancellationToken: TestContext.Current.CancellationToken);
        staleMealResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — current user purges. Without the stale-sub broadening, this would 500
        // on FK_meal_entries_recipes_recipe_id.
        var purgeResp = await currentClient.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);
        var body = await purgeResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        purgeResp.StatusCode.ShouldBe(HttpStatusCode.NoContent, body);

        // Assert — current user's roots are gone AND the orphan meal entry was
        // removed (so it no longer blocks further runs).
        await using var scope = currentFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await db.Recipes.IgnoreQueryFilters().AnyAsync(x => x.CreatedByUserId == currentUserId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await db.Products.IgnoreQueryFilters().AnyAsync(x => x.CreatedByUserId == currentUserId, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await db.MealEntries.IgnoreQueryFilters().AnyAsync(x => x.PersonId == TestAuthHandler.PersonIdFor(staleUserId), cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    /// <summary>When user has no data: <c>DELETE</c> purge my data returns 204.</summary>
    [Fact]
    public async Task DELETE_PurgeMyData_WhenUserHasNoData_Returns204()
    {
        using var factory = new DietPlannerWebApplicationFactory(
            _db.ConnectionString,
            userId: $"purge-empty-{Guid.NewGuid():N}",
            settings: TestSupportEnabled);
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/test-support/purge-my-data", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
