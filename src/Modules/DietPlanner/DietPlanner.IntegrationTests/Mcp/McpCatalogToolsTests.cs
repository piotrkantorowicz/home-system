namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Api.Mcp;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>Integration tests for the catalog MCP tools: visibility, details and paging match REST through real dispatchers and PostgreSQL.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpCatalogToolsTests(DatabaseFixture db) : IDisposable
{
    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[..8];

    /// <summary>Search returns the caller's own items and household-shared items, never another person's private ones, and keeps unknown nutrition null.</summary>
    [Fact]
    public async Task SearchProducts_ReturnsOwnAndSharedButNotForeignPrivate()
    {
        var (me, partner, stranger) = ($"cat-me-{_tag}", $"cat-partner-{_tag}", $"cat-stranger-{_tag}");
        _mcp.Households.Join(me, partner);
        await CreateProductAsync(me, $"Oats {_tag}", calories: null, "Private");
        await CreateProductAsync(partner, $"Rice {_tag}", calories: 130m, "Household");
        await CreateProductAsync(partner, $"Secret {_tag}", calories: 1m, "Private");
        await CreateProductAsync(stranger, $"Bread {_tag}", calories: 250m, "Household");

        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var page = await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = _tag });

        page.Items.Select(p => p.Name).Order().ShouldBe([$"Oats {_tag}", $"Rice {_tag}"]);
        page.Items.Single(p => p.Name.StartsWith("Oats", StringComparison.Ordinal)).CaloriesPer100g.ShouldBeNull();
        page.Items.Single(p => p.Name.StartsWith("Rice", StringComparison.Ordinal)).IsOwner.ShouldBeFalse();
    }

    /// <summary>A guessed id of someone else's private product fails exactly like a missing id.</summary>
    [Fact]
    public async Task GetProduct_WithForeignPrivateId_FailsLikeMissing()
    {
        var foreign = await CreateProductAsync($"cat-owner-{_tag}", $"Hidden {_tag}", 10m, "Private");
        await using var client = await _mcp.ConnectAsync($"cat-guesser-{_tag}", DietPlannerMcp.ReadScope);

        var hidden = await McpTestHost.CallExpectingErrorAsync(client, "get_product", new() { ["productId"] = foreign });
        var missing = await McpTestHost.CallExpectingErrorAsync(client, "get_product", new() { ["productId"] = Guid.CreateVersion7() });

        hidden.ShouldContain("does not exist or is not visible");
        missing.ShouldContain("does not exist or is not visible");
    }

    /// <summary>Recipe details expose servings, ingredients and instructions for planning and ingredient reuse.</summary>
    [Fact]
    public async Task GetRecipe_ReturnsServingsIngredientsAndInstructions()
    {
        var me = $"cat-cook-{_tag}";
        var productId = await CreateProductAsync(me, $"Lentils {_tag}", 116m, "Private");
        var recipeId = await CreateRecipeAsync(me, $"Dal {_tag}", productId);

        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var recipe = await McpTestHost.CallAsync<CatalogRecipeDto>(client, "get_recipe", new() { ["recipeId"] = recipeId });
        var found = await McpTestHost.CallAsync<CatalogPageDto<CatalogRecipeDto>>(client, "search_recipes", new() { ["query"] = $"Dal {_tag}" });

        recipe.Servings.ShouldBe(4);
        recipe.Instructions.ShouldBe("Simmer.");
        recipe.Ingredients.ShouldHaveSingleItem().ShouldBe(new CatalogIngredientDto(productId, $"Lentils {_tag}", 200m, "g"));
        found.Items.ShouldHaveSingleItem().Id.ShouldBe(recipeId);
    }

    /// <summary>Pages are bounded and stable.</summary>
    [Fact]
    public async Task SearchProducts_Paging_IsBoundedAndStable()
    {
        var me = $"cat-pager-{_tag}";
        foreach (var name in new[] { "A", "B", "C" })
            await CreateProductAsync(me, $"Page {_tag} {name}", 1m, "Private");
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var second = await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(
            client, "search_products", new() { ["query"] = $"Page {_tag}", ["page"] = 2, ["pageSize"] = 2 });
        var tooBig = await McpTestHost.CallExpectingErrorAsync(client, "search_products", new() { ["pageSize"] = 51 });
        var zero = await McpTestHost.CallExpectingErrorAsync(client, "search_recipes", new() { ["page"] = 0 });

        second.ShouldSatisfyAllConditions(
            p => p.TotalCount.ShouldBe(3),
            p => p.Items.Count.ShouldBe(1),
            p => p.HasNextPage.ShouldBeFalse());
        tooBig.ShouldContain("between 1 and 50");
        zero.ShouldContain("1 or greater");
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private async Task<Guid> CreateProductAsync(string subject, string name, decimal? calories, string visibility)
    {
        var response = await _mcp.Rest(subject).PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest(name, calories, null, null, null, null, "g", null, null, visibility), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return Guid.Parse((await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));
    }

    private async Task<Guid> CreateRecipeAsync(string subject, string name, Guid productId)
    {
        var response = await _mcp.Rest(subject).PostAsJsonAsync("/api/v1/recipes",
            new CreateRecipeRequest(name, null, "Simmer.", 4, 30, [new RecipeIngredientRequest(productId, 200m, "g")], "Private"),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return Guid.Parse((await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));
    }
}
