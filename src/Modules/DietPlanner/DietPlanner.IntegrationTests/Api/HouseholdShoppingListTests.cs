namespace DietPlanner.IntegrationTests.Api;

using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.GetShoppingList;
using DietPlanner.IntegrationTests.Infrastructure;
using global::Household.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// #223 — the shopping list is a shared household resource: it aggregates the planned
/// meals of every member, not just the caller's.
/// </summary>
public sealed class HouseholdShoppingListTests : IClassFixture<HouseholdShoppingListFixture>
{
    private readonly HouseholdShoppingListFixture _fx;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fx">The shared fixture.</param>
    public HouseholdShoppingListTests(HouseholdShoppingListFixture fx) => _fx = fx;

    private DietPlannerWebApplicationFactory FactoryFor(string subject)
        => new(
            _fx.DietPlannerConnectionString,
            subject,
            new Dictionary<string, string?> { ["ConnectionStrings:Household"] = _fx.HouseholdConnectionString });

    /// <summary><c>ShoppingList</c> aggregates every household members planned meals.</summary>
    [Fact]
    public async Task ShoppingList_AggregatesEveryHouseholdMembersPlannedMeals()
    {
        var ownerFactory = FactoryFor($"hh-owner-{Guid.NewGuid():N}");
        var memberFactory = FactoryFor($"hh-member-{Guid.NewGuid():N}");

        await ownerFactory.Services.MigrateHouseholdDatabaseAsync(NullLogger.Instance);

        var owner = ownerFactory.CreateClient();
        var member = memberFactory.CreateClient();
        var date = TestClock.Today;

        // Owner sets up a household; the member joins it.
        (await owner.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync("/api/households", new { name = "Shared Kitchen" }, cancellationToken: TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var householdId = (await owner.GetFromJsonAsync<HouseholdBody>("/api/households/me", cancellationToken: TestContext.Current.CancellationToken))!.Id;

        (await member.PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var memberPersonId = (await member.GetFromJsonAsync<PersonBody>("/api/persons/me", cancellationToken: TestContext.Current.CancellationToken))!.Id;
        var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members", new { personId = memberPersonId, role = "Adult", nickname = (string?)null }, cancellationToken: TestContext.Current.CancellationToken);
        add.EnsureSuccessStatusCode();
        var invitationId = (await add.Content.ReadFromJsonAsync<AddMemberResultBody>(cancellationToken: TestContext.Current.CancellationToken))!.InvitationId;
        (await member.PostAsync($"/api/households/invitations/{invitationId}/accept", null, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        // Each member plans one meal on the same day, using their own recipe/product.
        await PlanAMealAsync(owner, "Owner", date, ingredientGrams: 100m);
        await PlanAMealAsync(member, "Member", date, ingredientGrams: 250m);

        // The owner's shopping list includes both.
        var items = await owner.GetFromJsonAsync<List<ShoppingListItemDto>>($"/api/v1/meals/shopping-list?From={date:yyyy-MM-dd}&To={date:yyyy-MM-dd}", cancellationToken: TestContext.Current.CancellationToken);

        items.ShouldNotBeNull();
        items!.Count.ShouldBe(2);
        items.Select(i => i.TotalAmount).OrderBy(x => x).ShouldBe([100m, 250m]);
    }

    private static async Task PlanAMealAsync(HttpClient client, string label, DateOnly date, decimal ingredientGrams)
    {
        var slot = await client.PutAsJsonAsync(
            "/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, $"{label} Breakfast", "07:00")]));
        slot.EnsureSuccessStatusCode();
        var slotId = (await client.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule"))!.Slots[0].Id;

        var productResp = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"{label}-prod-{Guid.NewGuid():N}", 100m, 5m, 10m, 2m, 1m, "g", null, null));
        productResp.EnsureSuccessStatusCode();
        var productId = Guid.Parse((await productResp.Content.ReadAsStringAsync()).Trim('"'));

        var recipeResp = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeRequest(
                Name: $"{label}-recipe-{Guid.NewGuid():N}",
                Description: null,
                Instructions: null,
                Servings: 1,
                PrepTimeMinutes: 5,
                Ingredients: [new RecipeIngredientRequest(productId, ingredientGrams, "g")]));
        recipeResp.EnsureSuccessStatusCode();
        var recipeId = Guid.Parse((await recipeResp.Content.ReadAsStringAsync()).Trim('"'));

        (await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealEntryRequest(date, slotId, recipeId, 1m, null, null, 0))).EnsureSuccessStatusCode();
    }

    private sealed record HouseholdBody(Guid Id);

    private sealed record AddMemberResultBody(Guid InvitationId);

    private sealed record PersonBody(Guid Id);
}
