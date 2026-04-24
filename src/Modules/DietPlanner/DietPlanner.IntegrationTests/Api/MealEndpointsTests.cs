namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class MealEndpointsTests
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;

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
        return schedule!.Slots.First().Id;
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

    [Fact]
    public async Task GET_Meals_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task GET_NutritionSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals/nutrition-summary");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task POST_MealEntry_WithUnknownSlotId_Returns404()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"Test Recipe {Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(
                Date: DateOnly.FromDateTime(DateTime.UtcNow),
                MealSlotId: Guid.NewGuid(),
                RecipeId: recipeId,
                Servings: 1m,
                Notes: null,
                MealTime: null,
                SequenceOrder: 0));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_MealSchedule_DeletingSlotWithEntries_Returns422()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"Test Recipe {Guid.NewGuid():N}");

        var mealResp = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(
                Date: DateOnly.FromDateTime(DateTime.UtcNow),
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

    [Fact]
    public async Task PUT_MealSchedule_RenamingSlotInPlace_PreservesEntries()
    {
        var client = FreshClient($"meal-{Guid.NewGuid():N}");
        var slotId = await EnsureBreakfastSlotAsync(client);
        var recipeId = await CreateRecipeAsync(client, $"Test Recipe {Guid.NewGuid():N}");

        var mealResp = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(
                Date: DateOnly.FromDateTime(DateTime.UtcNow),
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
