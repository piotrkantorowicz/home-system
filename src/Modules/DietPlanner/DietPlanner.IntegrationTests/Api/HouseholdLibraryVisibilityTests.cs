namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.SearchProducts;
using DietPlanner.Application.Queries.SearchRecipes;
using DietPlanner.IntegrationTests.Infrastructure;
using global::Household.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Abstractions.Core.Pagination;

/// <summary>
/// #230 — recipes and products carry a visibility: <c>Private</c> (creator only), <c>Household</c>
/// (the creator's household, default) or <c>Public</c> (everyone). The creator always edits;
/// an Owner/Adult of the creator's household also edits a non-private item.
/// </summary>
public sealed class HouseholdLibraryVisibilityTests : IClassFixture<HouseholdShoppingListFixture>
{
    private readonly HouseholdShoppingListFixture _fx;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fx">The shared fixture.</param>
    public HouseholdLibraryVisibilityTests(HouseholdShoppingListFixture fx) => _fx = fx;

    /// <summary>A recipe / product created without a visibility is stored as <c>Household</c>.</summary>
    [Fact]
    public async Task POST_WithoutVisibility_StoresHousehold()
    {
        var hh = await SetUpAsync();
        var productId = await CreateProductAsync(hh.Owner, null);
        var recipeId = await CreateRecipeAsync(hh.Owner, productId, null);

        var product = await GetAsync<ProductDto>(hh.Owner, $"/api/v1/products/{productId}");
        var recipe = await GetAsync<RecipeDto>(hh.Owner, $"/api/v1/recipes/{recipeId}");

        product.Visibility.ShouldBe("Household");
        recipe.Visibility.ShouldBe("Household");
    }

    /// <summary>A private item is invisible to another member of the same household (search and get-by-id).</summary>
    [Fact]
    public async Task Private_IsInvisibleToHousemate()
    {
        var hh = await SetUpAsync();
        var productId = await CreateProductAsync(hh.Owner, "Private");
        var recipeId = await CreateRecipeAsync(hh.Owner, productId, "Private");

        (await hh.Adult.GetAsync($"/api/v1/recipes/{recipeId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await hh.Adult.GetAsync($"/api/v1/products/{productId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SearchRecipesAsync(hh.Adult)).ShouldNotContain(r => r.Id == recipeId);
        (await SearchProductsAsync(hh.Adult)).ShouldNotContain(p => p.Id == productId);
    }

    /// <summary>A household item is visible to members and invisible to a user from another household.</summary>
    [Fact]
    public async Task Household_IsVisibleToMembersOnly()
    {
        var hh = await SetUpAsync();
        var recipeId = await CreateRecipeAsync(hh.Owner, await CreateProductAsync(hh.Owner, null), "Household");

        (await SearchRecipesAsync(hh.Child)).ShouldContain(r => r.Id == recipeId);
        (await hh.Outsider.GetAsync($"/api/v1/recipes/{recipeId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await SearchRecipesAsync(hh.Outsider)).ShouldNotContain(r => r.Id == recipeId);
    }

    /// <summary>A public item is visible to another household, which still cannot edit it.</summary>
    [Fact]
    public async Task Public_IsVisibleButReadOnlyToOutsider()
    {
        var hh = await SetUpAsync();
        var productId = await CreateProductAsync(hh.Owner, "Public");

        var product = await GetAsync<ProductDto>(hh.Outsider, $"/api/v1/products/{productId}");
        var update = await hh.Outsider.PutAsJsonAsync($"/api/v1/products/{productId}",
            new UpdateProductRequest("Hijacked", 1m, 1m, 1m, 1m, 1m, "g", null, null), Ct);

        product.CanEdit.ShouldBeFalse();
        update.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>An Adult edits and deletes another member's household item; a Child gets 403.</summary>
    [Fact]
    public async Task HouseholdItem_AdultEdits_ChildForbidden()
    {
        var hh = await SetUpAsync();
        var productId = await CreateProductAsync(hh.Owner, null);
        var recipeId = await CreateRecipeAsync(hh.Owner, productId, null);
        var body = new UpdateRecipeRequest($"renamed-{Guid.NewGuid():N}", null, null, 2, null,
            [new RecipeIngredientRequest(productId, 50m, "g")]);

        var childUpdate = await hh.Child.PutAsJsonAsync($"/api/v1/recipes/{recipeId}", body, Ct);
        var childDelete = await hh.Child.DeleteAsync($"/api/v1/recipes/{recipeId}", Ct);
        var adultUpdate = await hh.Adult.PutAsJsonAsync($"/api/v1/recipes/{recipeId}", body, Ct);
        var adultDelete = await hh.Adult.DeleteAsync($"/api/v1/products/{productId}", Ct);

        childUpdate.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        childDelete.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        adultUpdate.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        adultDelete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>Only the creator changes visibility; an Adult trying to make a housemate's item private gets 403.</summary>
    [Fact]
    public async Task PUT_VisibilityChangeByNonCreator_Returns403()
    {
        var hh = await SetUpAsync();
        var productId = await CreateProductAsync(hh.Owner, null);

        var response = await hh.Adult.PutAsJsonAsync($"/api/v1/products/{productId}",
            new UpdateProductRequest($"p-{Guid.NewGuid():N}", 1m, 1m, 1m, 1m, 1m, "g", null, null, "Private"), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Overriding a meal with a housemate's household recipe succeeds; with their private recipe it is 404.</summary>
    [Fact]
    public async Task PATCH_Override_WithHousematesRecipe_ByVisibility()
    {
        var hh = await SetUpAsync();
        var mealId = await PlanOwnMealAsync(hh.Adult);
        var productId = await CreateProductAsync(hh.Owner, null);
        var shared = await CreateRecipeAsync(hh.Owner, productId, "Household");
        var secret = await CreateRecipeAsync(hh.Owner, productId, "Private");

        var ok = await hh.Adult.PatchAsJsonAsync($"/api/v1/meals/{mealId}/override", new OverrideMealEntryRequest(shared, []), Ct);
        var hidden = await hh.Adult.PatchAsJsonAsync($"/api/v1/meals/{mealId}/override", new OverrideMealEntryRequest(secret, []), Ct);

        ok.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        hidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>A household recipe using a private product still shows that ingredient and counts its macros.</summary>
    [Fact]
    public async Task GET_HouseholdRecipeWithPrivateProduct_KeepsIngredientAndMacros()
    {
        var hh = await SetUpAsync();
        var productId = await CreateProductAsync(hh.Owner, "Private");
        var recipeId = await CreateRecipeAsync(hh.Owner, productId, "Household");

        var recipe = await GetAsync<RecipeDto>(hh.Adult, $"/api/v1/recipes/{recipeId}");

        recipe.Ingredients.ShouldHaveSingleItem().ProductName.ShouldStartWith("lib-prod-");
        recipe.TotalNutrition.ShouldNotBeNull().Calories.ShouldBe(200m);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private DietPlannerWebApplicationFactory FactoryFor(string subject)
        => new(
            _fx.DietPlannerConnectionString,
            subject,
            new Dictionary<string, string?> { ["ConnectionStrings:Household"] = _fx.HouseholdConnectionString });

    private async Task<Home> SetUpAsync()
    {
        var tag = Guid.NewGuid().ToString("N");
        var ownerFactory = FactoryFor($"lib-owner-{tag}");
        await ownerFactory.Services.MigrateHouseholdDatabaseAsync(NullLogger.Instance);

        var owner = await NewHouseholdAsync(ownerFactory.CreateClient());
        var householdId = (await owner.GetFromJsonAsync<IdBody>("/api/households/me", Ct))!.Id;

        async Task<HttpClient> JoinAsync(string role)
        {
            var client = FactoryFor($"lib-{role.ToLowerInvariant()}-{tag}").CreateClient();
            (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
            var personId = (await client.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
            var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members",
                new { personId, role, nickname = (string?)null }, Ct);
            add.EnsureSuccessStatusCode();
            var invitationId = (await add.Content.ReadFromJsonAsync<InvitationBody>(Ct))!.InvitationId;
            (await client.PostAsync($"/api/households/invitations/{invitationId}/accept", null, Ct)).EnsureSuccessStatusCode();
            return client;
        }

        var adult = await JoinAsync("Adult");
        var child = await JoinAsync("Child");
        var outsider = await NewHouseholdAsync(FactoryFor($"lib-outsider-{tag}").CreateClient());
        return new Home(owner, adult, child, outsider);
    }

    private static async Task<HttpClient> NewHouseholdAsync(HttpClient client)
    {
        (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/households", new { name = "Library Home" }, Ct)).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string? visibility)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest($"lib-prod-{Guid.NewGuid():N}", 100m, 5m, 10m, 2m, 1m, "g", null, null, visibility), Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(Ct);
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client, Guid productId, string? visibility)
    {
        var response = await client.PostAsJsonAsync("/api/v1/recipes",
            new CreateRecipeRequest($"lib-recipe-{Guid.NewGuid():N}", null, null, 1, null,
                [new RecipeIngredientRequest(productId, 200m, "g")], visibility), Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(Ct);
    }

    private static async Task<Guid> PlanOwnMealAsync(HttpClient client)
    {
        (await client.PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Lunch", "12:00")]), Ct)).EnsureSuccessStatusCode();
        var schedule = await client.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule", Ct);
        var recipeId = await CreateRecipeAsync(client, await CreateProductAsync(client, null), null);
        var response = await client.PostAsJsonAsync("/api/v1/meals",
            new CreateMealEntryRequest(TestClock.Today, schedule!.Slots[0].Id, recipeId, 1m, null, null, 0, null), Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(Ct);
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<T>(Ct))!;
    }

    private static async Task<IReadOnlyList<RecipeDto>> SearchRecipesAsync(HttpClient client)
        => (await GetAsync<PagedList<RecipeDto>>(client, "/api/v1/recipes?search=lib-recipe-&pageSize=100")).Items;

    private static async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(HttpClient client)
        => (await GetAsync<PagedList<ProductDto>>(client, "/api/v1/products?search=lib-prod-&pageSize=100")).Items;

    private sealed record Home(HttpClient Owner, HttpClient Adult, HttpClient Child, HttpClient Outsider);

    private sealed record IdBody(Guid Id);

    private sealed record InvitationBody(Guid InvitationId);
}
