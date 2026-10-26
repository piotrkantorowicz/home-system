namespace DietPlanner.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.SearchRecipes;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>update_recipe</c> and <c>delete_recipe</c>: REST-equivalent results and
/// ownership rules through real dispatchers and PostgreSQL.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpRecipeEditToolsTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>An update over MCP replaces fields and ingredients, and the result equals what REST reads back.</summary>
    [Fact]
    public async Task UpdateRecipe_ResultMatchesRest()
    {
        var me = $"re-update-{_tag}";
        var (oats, milk) = (await ProductAsync(me, $"Oats {_tag}", 380m), await ProductAsync(me, $"Milk {_tag}", 64m));
        var id = await RecipeAsync(me, $"Porridge {_tag}", "Private", oats);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var updated = await McpTestHost.CallAsync<CatalogRecipeDto>(client, "update_recipe",
            Args(id, $"Milk porridge {_tag}", 2, (oats, 80m, "g"), (milk, 300m, "ml")));
        var rest = await _mcp.Rest(me).GetFromJsonAsync<RecipeDto>($"/api/v1/recipes/{id}", TestContext.Current.CancellationToken);

        updated.ShouldBe(CatalogRecipeDto.From(rest!), new RecipeComparer());
        updated.ShouldSatisfyAllConditions(
            r => r.Name.ShouldBe($"Milk porridge {_tag}"),
            r => r.Servings.ShouldBe(2),
            r => r.Ingredients.Select(i => i.ProductId).ShouldBe([oats, milk], ignoreOrder: true),
            r => r.Visibility.ShouldBe("Private"));
    }

    /// <summary>A household adult may edit a shared recipe but not change who sees it, and may not add a product they cannot see.</summary>
    [Fact]
    public async Task UpdateRecipe_ByHouseholdAdult_EditsWithinTheRules()
    {
        var (owner, partner, stranger) = ($"re-owner-{_tag}", $"re-partner-{_tag}", $"re-stranger-{_tag}");
        _mcp.Households.Join(owner, partner);
        var rice = await ProductAsync(owner, $"Rice {_tag}", 130m, "Household");
        var hidden = await ProductAsync(stranger, $"Hidden {_tag}", 10m);
        var id = await RecipeAsync(owner, $"Rice bowl {_tag}", "Household", rice);
        await using var client = await _mcp.ConnectAsync(partner, WriteScopes);

        var edited = await McpTestHost.CallAsync<CatalogRecipeDto>(client, "update_recipe", Args(id, $"Big rice bowl {_tag}", 1, (rice, 150m, "g")));
        var visibility = await McpTestHost.CallExpectingErrorAsync(client, "update_recipe",
            Args(id, $"Big rice bowl {_tag}", 1, [(rice, 150m, "g")], "Private"));
        var foreign = await McpTestHost.CallExpectingErrorAsync(client, "update_recipe", Args(id, $"Stolen {_tag}", 1, (hidden, 10m, "g")));

        edited.Name.ShouldBe($"Big rice bowl {_tag}");
        visibility.ShouldContain("Only the creator");
        foreign.ShouldContain("not found", Case.Insensitive);
        (await GetAsync(owner, id)).Name.ShouldBe($"Big rice bowl {_tag}");
    }

    /// <summary>Zero servings or an empty ingredient list come back as field errors and change nothing.</summary>
    [Theory]
    [InlineData(0, true, "Servings")]
    [InlineData(1, false, "Ingredients")]
    public async Task UpdateRecipe_WithInvalidInput_ReturnsValidationError(int servings, bool withIngredient, string field)
    {
        var me = $"re-invalid-{_tag}";
        var flour = await ProductAsync(me, $"Flour {_tag}", 364m);
        var id = await RecipeAsync(me, $"Bread {_tag}", "Private", flour);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var error = await McpTestHost.CallExpectingErrorAsync(client, "update_recipe",
            Args(id, $"Changed {_tag}", servings, withIngredient ? [(flour, 100m, "g")] : []));

        error.ShouldContain(field);
        (await GetAsync(me, id)).Name.ShouldBe($"Bread {_tag}");
    }

    /// <summary>What the description promises after a delete: gone from search, cannot be planned, planned meals keep it.</summary>
    [Fact]
    public async Task DeleteRecipe_RemovesItButPlannedMealsKeepIt()
    {
        var me = $"re-delete-{_tag}";
        var rest = _mcp.Rest(me);
        var recipe = $"Curry {_tag}";
        var import = new ImportDto(
            [new ImportProductDto($"Paneer {_tag}", 300m, 18m, 4m, 22m, 0m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto($"Paneer {_tag}", 200m, "g")])],
            [new ImportScheduleDto("2026-11-05", [new ImportMealDto("Lunch", recipe, 1m, null)])]);
        (await rest.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var meal = (await rest.GetFromJsonAsync<IReadOnlyList<MealEntryDto>>("/api/v1/meals?from=2026-11-05&to=2026-11-05", TestContext.Current.CancellationToken))!.Single();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(client, "delete_recipe", new() { ["recipeId"] = meal.RecipeId });
        var search = await McpTestHost.CallAsync<CatalogPageDto<CatalogRecipeDto>>(client, "search_recipes", new() { ["query"] = recipe });
        var get = await rest.GetAsync($"/api/v1/recipes/{meal.RecipeId}", TestContext.Current.CancellationToken);
        var plan = await rest.PostAsJsonAsync("/api/v1/meals",
            new CreateMealEntryRequest(new DateOnly(2026, 11, 6), meal.MealSlotId, meal.RecipeId, 1m, null, null, null), TestContext.Current.CancellationToken);
        var diary = (await McpTestHost.CallAsync<IReadOnlyList<DiaryMealDto>>(client, "get_meal_entries", new() { ["from"] = "2026-11-05", ["to"] = "2026-11-05" })).Single();
        var again = await McpTestHost.CallExpectingErrorAsync(client, "delete_recipe", new() { ["recipeId"] = meal.RecipeId });

        deleted.Id.ShouldBe(meal.RecipeId);
        search.Items.ShouldBeEmpty();
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        plan.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        diary.RecipeName.ShouldBe(recipe);
        again.ShouldContain("not found", Case.Insensitive);
    }

    /// <summary>A household adult may delete a shared recipe; a guest of the household may not, and it stays.</summary>
    [Fact]
    public async Task DeleteRecipe_HouseholdAdultAllowedGuestRefused()
    {
        var (owner, adult, guest) = ($"re-hh-owner-{_tag}", $"re-hh-adult-{_tag}", $"re-hh-guest-{_tag}");
        _mcp.Households.Join(owner, adult, guest);
        _mcp.Households.Contexts[guest] = _mcp.Households.Contexts[guest] with { Role = "Guest" };
        var egg = await ProductAsync(owner, $"Egg {_tag}", 143m, "Household");
        var byAdult = await RecipeAsync(owner, $"Omelette {_tag}", "Household", egg);
        var byGuest = await RecipeAsync(owner, $"Frittata {_tag}", "Household", egg);
        await using var adultClient = await _mcp.ConnectAsync(adult, WriteScopes);
        await using var guestClient = await _mcp.ConnectAsync(guest, WriteScopes);

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(adultClient, "delete_recipe", new() { ["recipeId"] = byAdult });
        var refused = await McpTestHost.CallExpectingErrorAsync(guestClient, "delete_recipe", new() { ["recipeId"] = byGuest });

        deleted.Id.ShouldBe(byAdult);
        (await _mcp.Rest(owner).GetAsync($"/api/v1/recipes/{byAdult}", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        refused.ShouldContain("adult of its household");
        (await GetAsync(owner, byGuest)).Name.ShouldBe($"Frittata {_tag}");
    }

    /// <summary>Another person cannot see, change or delete the caller's private recipe; it stays untouched.</summary>
    [Fact]
    public async Task EditTools_ForStranger_FailLikeMissingAndChangeNothing()
    {
        var (me, stranger) = ($"re-me-{_tag}", $"re-stranger2-{_tag}");
        var honey = await ProductAsync(me, $"Honey {_tag}", 304m);
        var id = await RecipeAsync(me, $"Tea {_tag}", "Private", honey);
        var theirs = await ProductAsync(stranger, $"Lemon {_tag}", 29m);
        await using var client = await _mcp.ConnectAsync(stranger, WriteScopes);

        var update = await McpTestHost.CallExpectingErrorAsync(client, "update_recipe", Args(id, $"Stolen {_tag}", 1, (theirs, 10m, "g")));
        var delete = await McpTestHost.CallExpectingErrorAsync(client, "delete_recipe", new() { ["recipeId"] = id });

        update.ShouldContain("not found", Case.Insensitive);
        delete.ShouldContain("not found", Case.Insensitive);
        (await GetAsync(me, id)).Name.ShouldBe($"Tea {_tag}");
    }

    /// <summary>A read-only token neither lists nor runs the edit tools.</summary>
    [Theory]
    [InlineData("update_recipe")]
    [InlineData("delete_recipe")]
    public async Task EditTools_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"re-ro-{_tag}";
        var salt = await ProductAsync(me, $"Salt {_tag}", 0m);
        var id = await RecipeAsync(me, $"Brine {_tag}", "Private", salt);
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, Args(id, $"Changed {_tag}", 1, (salt, 5m, "g")), cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await GetAsync(me, id)).Name.ShouldBe($"Brine {_tag}");
    }

    /// <summary>The assistant can run the edit tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task EditTools_InProcess_UpdateAndDelete()
    {
        var me = $"re-inproc-{_tag}";
        var lentils = await ProductAsync(me, $"Lentils {_tag}", 116m);
        var id = await RecipeAsync(me, $"Dal {_tag}", "Private", lentils);
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var update = await invoker.InvokeAsync(user, "update_recipe", Json(Args(id, $"Red dal {_tag}", 1, (lentils, 200m, "g"))), TestContext.Current.CancellationToken);
        var renamed = await GetAsync(me, id);
        var delete = await invoker.InvokeAsync(user, "delete_recipe", Json(new() { ["recipeId"] = id }), TestContext.Current.CancellationToken);
        var gone = await _mcp.Rest(me).GetAsync($"/api/v1/recipes/{id}", TestContext.Current.CancellationToken);

        update.IsError.ShouldNotBe(true);
        renamed.Name.ShouldBe($"Red dal {_tag}");
        delete.IsError.ShouldNotBe(true);
        gone.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Args(Guid id, string name, int servings, params (Guid Product, decimal Amount, string Unit)[] ingredients)
        => Args(id, name, servings, ingredients, null);

    private static Dictionary<string, object?> Args(
        Guid id, string name, int servings, (Guid Product, decimal Amount, string Unit)[] ingredients, string? visibility)
    {
        var args = new Dictionary<string, object?>
        {
            ["recipeId"] = id,
            ["name"] = name,
            ["servings"] = servings,
            ["ingredients"] = ingredients.Select(i => new { productId = i.Product, amount = i.Amount, unit = i.Unit }).ToArray(),
        };
        if (visibility is not null)
            args["visibility"] = visibility;
        return args;
    }

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));

    private async Task<Guid> ProductAsync(string subject, string name, decimal calories, string visibility = "Private")
    {
        var response = await _mcp.Rest(subject).PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest(name, calories, null, null, null, null, "g", null, null, visibility), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return Guid.Parse((await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));
    }

    private async Task<Guid> RecipeAsync(string subject, string name, string visibility, Guid product)
    {
        var response = await _mcp.Rest(subject).PostAsJsonAsync("/api/v1/recipes",
            new CreateRecipeRequest(name, null, null, 1, null, [new RecipeIngredientRequest(product, 100m, "g")], visibility), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return Guid.Parse((await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));
    }

    private async Task<RecipeDto> GetAsync(string subject, Guid id)
        => (await _mcp.Rest(subject).GetFromJsonAsync<RecipeDto>($"/api/v1/recipes/{id}", TestContext.Current.CancellationToken))!;

    /// <summary>Records holding lists compare by reference; compare the lists by content.</summary>
    private sealed class RecipeComparer : IEqualityComparer<CatalogRecipeDto>
    {
        public bool Equals(CatalogRecipeDto? x, CatalogRecipeDto? y)
            => x is not null && y is not null
                && x with { Ingredients = [] } == y with { Ingredients = [] }
                && x.Ingredients.SequenceEqual(y.Ingredients);

        public int GetHashCode(CatalogRecipeDto obj) => obj.Id.GetHashCode();
    }
}
