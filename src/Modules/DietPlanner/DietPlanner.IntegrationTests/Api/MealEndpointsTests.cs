namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.GetShoppingList;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>Meal</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class MealEndpointsTests
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public MealEndpointsTests(DatabaseFixture db)
    {
        _db = db;
        _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();
    }

    private HttpClient FreshClient(string userId)
        => new DietPlannerWebApplicationFactory(_db.ConnectionString, userId).CreateClient();

    private static async Task<Guid> EnsureBreakfastSlotAsync(HttpClient client)
    {
        var put = await client.PutAsJsonAsync(
            "/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]));
        put.EnsureSuccessStatusCode();

        var schedule = await client.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule");
        return schedule!.Slots[0].Id;
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client, string name)
    {
        var productResp = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"{name}-prod", 100m, 5m, 10m, 2m, 1m, "g", null, null));
        productResp.EnsureSuccessStatusCode();
        var productId = Guid.Parse((await productResp.Content.ReadAsStringAsync()).Trim('"'));

        var recipeResp = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeRequest(
                Name: name,
                Description: null,
                Instructions: null,
                Servings: 1,
                PrepTimeMinutes: 5,
                Ingredients: [new RecipeIngredientRequest(productId, 80m, "g")]));
        recipeResp.EnsureSuccessStatusCode();
        return Guid.Parse((await recipeResp.Content.ReadAsStringAsync()).Trim('"'));
    }

    /// <summary><c>GET</c> meals returns ok.</summary>
    [Fact]
    public async Task GET_Meals_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    /// <summary><c>GET</c> nutrition summary returns ok.</summary>
    [Fact]
    public async Task GET_NutritionSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals/nutrition-summary");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    /// <summary>With unknown slot id: <c>POST</c> meal entry returns 404.</summary>
    [Fact]
    public async Task POST_MealEntry_WithUnknownSlotId_Returns404()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"Test Recipe {Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(
                Date: TestClock.Today,
                MealSlotId: Guid.NewGuid(),
                RecipeId: recipeId,
                Servings: 1m,
                Notes: null,
                MealTime: null,
                SequenceOrder: 0));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Deleting slot with entries: <c>PUT</c> meal schedule returns 422.</summary>
    [Fact]
    public async Task PUT_MealSchedule_DeletingSlotWithEntries_Returns422()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"Test Recipe {Guid.NewGuid():N}");

        var mealResp = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(
                Date: TestClock.Today,
                MealSlotId: slotId,
                RecipeId: recipeId,
                Servings: 1m,
                Notes: null,
                MealTime: null,
                SequenceOrder: 0));
        mealResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Now try to replace the schedule with a different slot — should fail because
        // the original slot has an entry.
        var deleteResp = await client.PutAsJsonAsync(
            "/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Lunch", "12:00")]));

        deleteResp.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary><c>GET</c> meals computes non zero nutrition from recipe ingredients.</summary>
    [Fact]
    public async Task GET_Meals_ComputesNonZeroNutritionFromRecipeIngredients()
    {
        // Regression: GetMealEntriesQueryHandler used to project r.Ingredients in EF,
        // which silently returned an empty list because Recipe exposes the navigation
        // as `IReadOnlyCollection<RecipeIngredient> => _ingredients.AsReadOnly()` —
        // EF can't translate AsReadOnly() in an expression tree. Per-meal calories
        // came back as 0 for every entry until the day view rendered them as "0 kcal".
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"NutritionRecipe-{Guid.NewGuid():N}");
        var date = TestClock.Today;

        var createResp = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(date, slotId, recipeId, 1m, null, null, 0));
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // CreateRecipeAsync seeds: product 100 kcal/100g, recipe with 80 g, servings=1.
        // Meal eats 1 serving → 80 g × 100 kcal/100 g × (1/1) = 80 kcal.
        var meals = await client.GetFromJsonAsync<List<MealEntryDto>>(
            $"/api/v1/meals?From={date:yyyy-MM-dd}&To={date:yyyy-MM-dd}");

        meals.ShouldNotBeNull();
        var meal = meals!.Single(m => m.RecipeId == recipeId);
        meal.Calories.ShouldBe(80m);
    }

    /// <summary><c>GET</c> shopping list returns ok.</summary>
    [Fact]
    public async Task GET_ShoppingList_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals/shopping-list");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    /// <summary><c>GET</c> shopping list aggregates planned ingredients across entries.</summary>
    [Fact]
    public async Task GET_ShoppingList_AggregatesPlannedIngredientsAcrossEntries()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"ShoppingRecipe-{Guid.NewGuid():N}");
        var date = TestClock.Today;

        // Recipe (servings=1) has one ingredient at 80g. Two meal entries: 1 serving + 2 servings.
        // Expect aggregated total = 80 * 1 + 80 * 2 = 240g for the single product.
        var first = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(date, slotId, recipeId, 1m, null, null, 0));
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(date, slotId, recipeId, 2m, null, null, 1));
        second.StatusCode.ShouldBe(HttpStatusCode.Created);

        var items = await client.GetFromJsonAsync<List<ShoppingListItemDto>>(
            $"/api/v1/meals/shopping-list?From={date:yyyy-MM-dd}&To={date:yyyy-MM-dd}");

        items.ShouldNotBeNull();
        items!.Count.ShouldBe(1);

        var item = items.Single();
        item.TotalAmount.ShouldBe(240m);
        item.Unit.ShouldBe("g");
        item.ProductName.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary><c>GET</c> shopping list ignores overrides and uses planned recipe.</summary>
    [Fact]
    public async Task GET_ShoppingList_IgnoresOverridesAndUsesPlannedRecipe()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var plannedRecipeId = await CreateRecipeAsync(client, $"PlannedRecipe-{Guid.NewGuid():N}");
        var date = TestClock.Today;

        var mealResp = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(date, slotId, plannedRecipeId, 1m, null, null, 0));
        mealResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var mealId = Guid.Parse((await mealResp.Content.ReadAsStringAsync()).Trim('"'));

        // Override with ad-hoc products — should be ignored by shopping list.
        var replacementRecipeId = await CreateRecipeAsync(client, $"ReplacementRecipe-{Guid.NewGuid():N}");
        var overrideResp = await client.PatchAsJsonAsync(
            $"/api/v1/meals/{mealId}/override",
            new OverrideMealEntryRequest(replacementRecipeId, []));
        overrideResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var items = await client.GetFromJsonAsync<List<ShoppingListItemDto>>(
            $"/api/v1/meals/shopping-list?From={date:yyyy-MM-dd}&To={date:yyyy-MM-dd}");

        items.ShouldNotBeNull();
        // Only the planned recipe's ingredient should appear (80g), not the replacement.
        items!.Count.ShouldBe(1);
        items.Single().TotalAmount.ShouldBe(80m);
    }

    /// <summary><c>GET</c> shopping list empty range and returns empty.</summary>
    [Fact]
    public async Task GET_ShoppingList_EmptyRange_ReturnsEmpty()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var farFuture = TestClock.Today.AddYears(10);

        var items = await client.GetFromJsonAsync<List<ShoppingListItemDto>>(
            $"/api/v1/meals/shopping-list?From={farFuture:yyyy-MM-dd}&To={farFuture:yyyy-MM-dd}");

        items.ShouldNotBeNull();
        items!.ShouldBeEmpty();
    }

    /// <summary>Renaming slot in place: <c>PUT</c> meal schedule preserves entries.</summary>
    [Fact]
    public async Task PUT_MealSchedule_RenamingSlotInPlace_PreservesEntries()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"Test Recipe {Guid.NewGuid():N}");

        var mealResp = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(
                Date: TestClock.Today,
                MealSlotId: slotId,
                RecipeId: recipeId,
                Servings: 1m,
                Notes: null,
                MealTime: null,
                SequenceOrder: 0));
        mealResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var renameResp = await client.PutAsJsonAsync(
            "/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(slotId, "Brunch", "10:00")]));
        renameResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var schedule = await client.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule");
        schedule!.Slots.Single().Id.ShouldBe(slotId);
        schedule.Slots.Single().Name.ShouldBe("Brunch");
    }
}
