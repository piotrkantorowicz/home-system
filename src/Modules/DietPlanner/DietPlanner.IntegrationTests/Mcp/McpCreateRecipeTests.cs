namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using ModelContextProtocol.Client;

/// <summary>Integration tests for <c>create_recipe</c>: authorized product references, quantity rules, replay and reuse by import.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpCreateRecipeTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>A recipe built from the caller's own and household-shared products can be read and searched afterwards.</summary>
    [Fact]
    public async Task CreateRecipe_FromOwnAndSharedProducts_IsReadableAndSearchable()
    {
        var (me, partner) = ($"cr-me-{_tag}", $"cr-partner-{_tag}");
        _mcp.Households.Join(me, partner);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var oats = await CreateProductAsync(client, $"Oats {_tag}");
        await using var partnerClient = await _mcp.ConnectAsync(partner, WriteScopes);
        var milk = await CreateProductAsync(partnerClient, $"Milk {_tag}");

        var created = await CreateRecipeAsync(client, Key(), $"Porridge {_tag}", servings: 2, (oats, 80m, "g"), (milk, 300m, "ml"));
        var recipe = await McpTestHost.CallAsync<CatalogRecipeDto>(client, "get_recipe", new() { ["recipeId"] = created.RecipeId });
        var found = await McpTestHost.CallAsync<CatalogPageDto<CatalogRecipeDto>>(client, "search_recipes", new() { ["query"] = $"Porridge {_tag}" });

        recipe.Servings.ShouldBe(2);
        recipe.Ingredients.Select(i => (i.ProductId, i.Amount, i.Unit)).ShouldBe([(oats, 80m, "g"), (milk, 300m, "ml")], ignoreOrder: true);
        found.Items.ShouldHaveSingleItem().Id.ShouldBe(created.RecipeId);
    }

    /// <summary>Someone else's private product and an unknown id fail identically and create nothing.</summary>
    [Fact]
    public async Task CreateRecipe_WithForeignPrivateOrUnknownProduct_FailsWithoutCreating()
    {
        await using var stranger = await _mcp.ConnectAsync($"cr-stranger-{_tag}", WriteScopes);
        var hidden = await CreateProductAsync(stranger, $"Hidden {_tag}", "Private");
        await using var client = await _mcp.ConnectAsync($"cr-thief-{_tag}", WriteScopes);

        var foreign = await McpTestHost.CallExpectingErrorAsync(client, "create_recipe", RecipeArgs(Key(), $"Stolen {_tag}", 1, (hidden, 10m, "g")));
        var unknown = await McpTestHost.CallExpectingErrorAsync(client, "create_recipe", RecipeArgs(Key(), $"Ghost {_tag}", 1, (Guid.CreateVersion7(), 10m, "g")));

        foreign.ShouldContain("not found", Case.Insensitive);
        unknown.ShouldContain("not found", Case.Insensitive);
        (await CountRecipesAsync(client, _tag)).ShouldBe(0);
    }

    /// <summary>Non-positive amounts, unsupported units and zero servings are rejected without partial creation.</summary>
    [Theory]
    [InlineData(0, "g", 1, "Amount")]
    [InlineData(-5, "g", 1, "Amount")]
    [InlineData(10, "handful", 1, "Unit")]
    [InlineData(10, "g", 0, "Servings")]
    public async Task CreateRecipe_WithInvalidQuantities_Fails(int amount, string unit, int servings, string field)
    {
        await using var client = await _mcp.ConnectAsync($"cr-invalid-{_tag}", WriteScopes);
        var flour = await CreateProductAsync(client, $"Flour {_tag}");

        var error = await McpTestHost.CallExpectingErrorAsync(client, "create_recipe", RecipeArgs(Key(), $"Bad {_tag}", servings, (flour, amount, unit)));

        error.ShouldContain(field);
        (await CountRecipesAsync(client, $"Bad {_tag}")).ShouldBe(0);
    }

    /// <summary>Retrying with the same key returns the same recipe; the same key with different fields conflicts.</summary>
    [Fact]
    public async Task CreateRecipe_Replay_ReturnsSameIdAndRejectsChangedFields()
    {
        var key = Key();
        await using var client = await _mcp.ConnectAsync($"cr-replay-{_tag}", WriteScopes);
        var rice = await CreateProductAsync(client, $"Rice {_tag}");

        var first = await CreateRecipeAsync(client, key, $"Rice bowl {_tag}", 1, (rice, 90m, "g"));
        var second = await CreateRecipeAsync(client, key, $"Rice bowl {_tag}", 1, (rice, 90m, "g"));
        var changed = await McpTestHost.CallExpectingErrorAsync(client, "create_recipe", RecipeArgs(key, $"Rice bowl {_tag}", 1, (rice, 120m, "g")));

        second.RecipeId.ShouldBe(first.RecipeId);
        changed.ShouldContain("already used for a different");
        (await CountRecipesAsync(client, $"Rice bowl {_tag}")).ShouldBe(1);
    }

    /// <summary>A recipe saved over MCP is reused by name when a meal plan is imported.</summary>
    [Fact]
    public async Task CreateRecipe_IsReusedByImport()
    {
        var me = $"cr-import-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var beans = await CreateProductAsync(client, $"Beans {_tag}");
        await CreateRecipeAsync(client, Key(), $"Chili {_tag}", 2, (beans, 400m, "g"));

        var import = new ImportDto([], [], [new ImportScheduleDto("2026-10-05", [new ImportMealDto("Lunch", $"Chili {_tag}", 1m, null)])]);
        var response = await _mcp.Rest(me).PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.ShouldBeTrue(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        result!.Stats.MealEntriesCreated.ShouldBe(1);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static string Key() => Guid.CreateVersion7().ToString();

    private static async Task<Guid> CreateProductAsync(McpClient client, string name, string? visibility = null)
        => (await McpTestHost.CallAsync<CreatedProductDto>(client, "create_product", new()
        {
            ["operationKey"] = Key(),
            ["name"] = name,
            ["unit"] = "g",
            ["visibility"] = visibility,
        })).ProductId;

    private static Dictionary<string, object?> RecipeArgs(string key, string name, int servings, params (Guid ProductId, decimal Amount, string Unit)[] ingredients)
        => new()
        {
            ["operationKey"] = key,
            ["name"] = name,
            ["servings"] = servings,
            ["ingredients"] = ingredients.Select(i => new { productId = i.ProductId, amount = i.Amount, unit = i.Unit }).ToArray(),
        };

    private static Task<CreatedRecipeDto> CreateRecipeAsync(
        McpClient client, string key, string name, int servings, params (Guid ProductId, decimal Amount, string Unit)[] ingredients)
        => McpTestHost.CallAsync<CreatedRecipeDto>(client, "create_recipe", RecipeArgs(key, name, servings, ingredients));

    private static async Task<int> CountRecipesAsync(McpClient client, string query)
        => (await McpTestHost.CallAsync<CatalogPageDto<CatalogRecipeDto>>(client, "search_recipes", new() { ["query"] = query })).TotalCount;
}
