namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.IntegrationTests.Infrastructure;
using global::Household.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// #229 — the meal plan is a shared household resource: members read each other's plan,
/// Owner/Adult plan for anyone (managed members included), Child for themselves, Guest for
/// no one; meal completion stays personal (self, or an adult for a managed member).
/// </summary>
public sealed class HouseholdMealPlanTests : IClassFixture<HouseholdShoppingListFixture>
{
    private static readonly DateOnly Today = TestClock.Today;

    private readonly HouseholdShoppingListFixture _fx;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fx">The shared fixture.</param>
    public HouseholdMealPlanTests(HouseholdShoppingListFixture fx) => _fx = fx;

    /// <summary><c>GET</c> meals for a member returns that member's entries with their name.</summary>
    [Fact]
    public async Task GET_Meals_ForHouseholdMember_ReturnsTheirEntries()
    {
        var hh = await SetUpHouseholdAsync();
        var slotId = await PutScheduleAsync(hh.Adult, null);
        await PlanAsync(hh.Adult, slotId, null, HttpStatusCode.Created);

        var meals = await hh.Owner.GetFromJsonAsync<List<MealEntryDto>>(
            $"/api/v1/meals?personId={hh.AdultId}", TestContext.Current.CancellationToken);

        meals.ShouldNotBeNull().ShouldHaveSingleItem().PersonId.ShouldBe(hh.AdultId);
        meals[0].PersonName.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary><c>GET</c> meals / schedule for someone outside the household returns 403.</summary>
    [Fact]
    public async Task GET_MealsAndSchedule_ForNonMember_Returns403()
    {
        var hh = await SetUpHouseholdAsync();
        var outsider = Guid.CreateVersion7();

        var meals = await hh.Owner.GetAsync($"/api/v1/meals?personId={outsider}", TestContext.Current.CancellationToken);
        var schedule = await hh.Owner.GetAsync($"/api/v1/meal-schedule?personId={outsider}", TestContext.Current.CancellationToken);

        meals.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>An adult sets up a managed child's schedule and plans, updates and deletes their meal.</summary>
    [Fact]
    public async Task Adult_PlansForManagedChild_FullLifecycleSucceeds()
    {
        var hh = await SetUpHouseholdAsync();
        var kidSlot = await PutScheduleAsync(hh.Adult, hh.KidId);

        var entryId = await PlanAsync(hh.Adult, kidSlot, hh.KidId, HttpStatusCode.Created);
        var kidMeals = await hh.Owner.GetFromJsonAsync<List<MealEntryDto>>(
            $"/api/v1/meals?personId={hh.KidId}", TestContext.Current.CancellationToken);
        kidMeals.ShouldNotBeNull().ShouldHaveSingleItem().Id.ShouldBe(entryId);

        var update = await hh.Adult.PutAsJsonAsync($"/api/v1/meals/{entryId}",
            new UpdateMealEntryRequest(Today, kidSlot, hh.RecipeId, 2m, "more", null, 0), TestContext.Current.CancellationToken);
        var complete = await hh.Adult.PatchAsync($"/api/v1/meals/{entryId}/complete", null, TestContext.Current.CancellationToken);
        var delete = await hh.Adult.DeleteAsync($"/api/v1/meals/{entryId}", TestContext.Current.CancellationToken);

        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        complete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>A child planning for someone else, and a guest planning at all, get 403.</summary>
    [Fact]
    public async Task ChildForOthers_AndGuest_Plan_Returns403()
    {
        var hh = await SetUpHouseholdAsync();
        var adultSlot = await PutScheduleAsync(hh.Adult, null);

        await PlanAsync(hh.Child, adultSlot, hh.AdultId, HttpStatusCode.Forbidden);
        await PlanAsync(hh.Guest, adultSlot, hh.AdultId, HttpStatusCode.Forbidden, hh.RecipeId);
        var guestSchedule = await hh.Guest.PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]), TestContext.Current.CancellationToken);

        guestSchedule.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A child may plan for themselves.</summary>
    [Fact]
    public async Task Child_PlansForSelf_Returns201()
    {
        var hh = await SetUpHouseholdAsync();
        var childSlot = await PutScheduleAsync(hh.Child, null);

        await PlanAsync(hh.Child, childSlot, null, HttpStatusCode.Created);
    }

    /// <summary>Completion is personal: an adult cannot complete another adult's meal, nor change their schedule.</summary>
    [Fact]
    public async Task Adult_CompletingOtherAdultsMeal_Returns403()
    {
        var hh = await SetUpHouseholdAsync();
        var adultSlot = await PutScheduleAsync(hh.Adult, null);
        var entryId = await PlanAsync(hh.Owner, adultSlot, hh.AdultId, HttpStatusCode.Created);

        var complete = await hh.Owner.PatchAsync($"/api/v1/meals/{entryId}/complete", null, TestContext.Current.CancellationToken);
        var schedule = await hh.Owner.PutAsJsonAsync($"/api/v1/meal-schedule?personId={hh.AdultId}",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Lunch", "12:00")]), TestContext.Current.CancellationToken);

        complete.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A slot from the planner's own schedule is rejected when planning for someone else.</summary>
    [Fact]
    public async Task POST_Meal_WithSlotOfAnotherPersonsSchedule_Returns404()
    {
        var hh = await SetUpHouseholdAsync();
        var ownerSlot = await PutScheduleAsync(hh.Owner, null);
        await PutScheduleAsync(hh.Adult, hh.KidId);

        await PlanAsync(hh.Owner, ownerSlot, hh.KidId, HttpStatusCode.NotFound);
    }

    private DietPlannerWebApplicationFactory FactoryFor(string subject)
        => new(
            _fx.DietPlannerConnectionString,
            subject,
            new Dictionary<string, string?> { ["ConnectionStrings:Household"] = _fx.HouseholdConnectionString });

    private async Task<Home> SetUpHouseholdAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var tag = Guid.NewGuid().ToString("N");
        var ownerFactory = FactoryFor($"mp-owner-{tag}");
        await ownerFactory.Services.MigrateHouseholdDatabaseAsync(NullLogger.Instance);

        var owner = ownerFactory.CreateClient();
        (await owner.PostAsync("/api/persons/me/sync", null, ct)).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync("/api/households", new { name = "Meal Plan Home" }, ct)).EnsureSuccessStatusCode();
        var householdId = (await owner.GetFromJsonAsync<IdBody>("/api/households/me", ct))!.Id;

        async Task<(HttpClient Client, Guid PersonId)> JoinAsync(string role)
        {
            var client = FactoryFor($"mp-{role.ToLowerInvariant()}-{tag}").CreateClient();
            (await client.PostAsync("/api/persons/me/sync", null, ct)).EnsureSuccessStatusCode();
            var personId = (await client.GetFromJsonAsync<IdBody>("/api/persons/me", ct))!.Id;
            var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members",
                new { personId, role, nickname = (string?)null }, ct);
            add.EnsureSuccessStatusCode();
            var invitationId = (await add.Content.ReadFromJsonAsync<InvitationBody>(ct))!.InvitationId;
            (await client.PostAsync($"/api/households/invitations/{invitationId}/accept", null, ct)).EnsureSuccessStatusCode();
            return (client, personId);
        }

        var adult = await JoinAsync("Adult");
        var child = await JoinAsync("Child");
        var guest = await JoinAsync("Guest");

        var kid = await owner.PostAsJsonAsync($"/api/households/{householdId}/managed-members",
            new { displayName = "Kid", email = (string?)null, role = "Child", nickname = (string?)null }, ct);
        kid.EnsureSuccessStatusCode();
        var kidId = Guid.Parse(kid.Headers.Location!.OriginalString.Split('/')[^1]);

        var recipeId = await CreateRecipeAsync(owner);
        return new Home(owner, adult.Client, adult.PersonId, child.Client, guest.Client, kidId, recipeId);
    }

    private static async Task<Guid> PutScheduleAsync(HttpClient client, Guid? personId)
    {
        var ct = TestContext.Current.CancellationToken;
        var query = personId is null ? "" : $"?personId={personId}";
        (await client.PutAsJsonAsync($"/api/v1/meal-schedule{query}",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]), ct)).EnsureSuccessStatusCode();
        var schedule = await client.GetFromJsonAsync<MealScheduleConfigDto>($"/api/v1/meal-schedule{query}", ct);
        return schedule!.Slots[0].Id;
    }

    private static async Task<Guid> PlanAsync(
        HttpClient client, Guid slotId, Guid? personId, HttpStatusCode expected, Guid? recipeId = null)
    {
        var ct = TestContext.Current.CancellationToken;
        recipeId ??= await CreateRecipeAsync(client); // a Guest cannot create one
        var response = await client.PostAsJsonAsync("/api/v1/meals",
            new CreateMealEntryRequest(Today, slotId, recipeId.Value, 1m, null, null, 0, personId), ct);

        response.StatusCode.ShouldBe(expected);
        return expected == HttpStatusCode.Created ? await response.Content.ReadFromJsonAsync<Guid>(ct) : Guid.Empty;
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client)
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await client.PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest($"mp-prod-{Guid.NewGuid():N}", 100m, 5m, 10m, 2m, 1m, "g", null, null), ct);
        product.EnsureSuccessStatusCode();
        var productId = await product.Content.ReadFromJsonAsync<Guid>(ct);

        var recipe = await client.PostAsJsonAsync("/api/v1/recipes",
            new CreateRecipeRequest($"mp-recipe-{Guid.NewGuid():N}", null, null, 1, 5,
                [new RecipeIngredientRequest(productId, 100m, "g")]), ct);
        recipe.EnsureSuccessStatusCode();
        return await recipe.Content.ReadFromJsonAsync<Guid>(ct);
    }

    private sealed record Home(
        HttpClient Owner, HttpClient Adult, Guid AdultId, HttpClient Child, HttpClient Guest, Guid KidId, Guid RecipeId);

    private sealed record IdBody(Guid Id);

    private sealed record InvitationBody(Guid InvitationId);
}
