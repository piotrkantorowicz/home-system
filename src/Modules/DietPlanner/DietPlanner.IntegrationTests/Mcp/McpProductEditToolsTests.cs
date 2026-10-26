namespace DietPlanner.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.SearchProducts;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>update_product</c> and <c>delete_product</c>: REST-equivalent results and
/// ownership rules through real dispatchers and PostgreSQL.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpProductEditToolsTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>An update over MCP leaves the product exactly as REST reads it back, unknown nutrition staying null.</summary>
    [Fact]
    public async Task UpdateProduct_ResultMatchesRest()
    {
        var me = $"pe-update-{_tag}";
        var id = await CreateAsync(me, $"Oats {_tag}", 380m, "Private");
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var updated = await McpTestHost.CallAsync<CatalogProductDto>(client, "update_product", new()
        {
            ["productId"] = id,
            ["name"] = $"Rolled oats {_tag}",
            ["unit"] = "g",
            ["caloriesPer100g"] = 372m,
            ["proteinPer100g"] = 13m,
        });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<ProductDto>($"/api/v1/products/{id}", TestContext.Current.CancellationToken);

        updated.ShouldBe(CatalogProductDto.From(rest!));
        updated.ShouldSatisfyAllConditions(
            p => p.Name.ShouldBe($"Rolled oats {_tag}"),
            p => p.CaloriesPer100g.ShouldBe(372m),
            p => p.FatPer100g.ShouldBeNull(),
            p => p.Visibility.ShouldBe("Private"));
    }

    /// <summary>A household adult may edit a shared product, as in the app, but may not change who sees it.</summary>
    [Fact]
    public async Task UpdateProduct_ByHouseholdAdult_EditsButCannotChangeVisibility()
    {
        var (owner, partner) = ($"pe-owner-{_tag}", $"pe-partner-{_tag}");
        _mcp.Households.Join(owner, partner);
        var id = await CreateAsync(owner, $"Rice {_tag}", 130m, "Household");
        await using var client = await _mcp.ConnectAsync(partner, WriteScopes);

        var edited = await McpTestHost.CallAsync<CatalogProductDto>(client, "update_product", Args(id, $"Basmati {_tag}"));
        var error = await McpTestHost.CallExpectingErrorAsync(client, "update_product", Args(id, $"Basmati {_tag}", "Private"));

        edited.Name.ShouldBe($"Basmati {_tag}");
        error.ShouldContain("Only the creator");
    }

    /// <summary>Invalid input comes back as a field error the agent can fix, and changes nothing.</summary>
    [Fact]
    public async Task UpdateProduct_WithEmptyName_ReturnsValidationError()
    {
        var me = $"pe-invalid-{_tag}";
        var id = await CreateAsync(me, $"Milk {_tag}", 64m, "Private");
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var error = await McpTestHost.CallExpectingErrorAsync(client, "update_product", Args(id, " "));

        error.ShouldContain("Name");
        (await GetAsync(me, id)).Name.ShouldBe($"Milk {_tag}");
    }

    /// <summary>Delete over MCP removes the product the way REST does: gone from search and from direct reads.</summary>
    [Fact]
    public async Task DeleteProduct_RemovesItLikeRest()
    {
        var me = $"pe-delete-{_tag}";
        var id = await CreateAsync(me, $"Butter {_tag}", 717m, "Private");
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(client, "delete_product", new() { ["productId"] = id });
        var rest = await _mcp.Rest(me).GetAsync($"/api/v1/products/{id}", TestContext.Current.CancellationToken);
        var search = await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = _tag });
        var again = await McpTestHost.CallExpectingErrorAsync(client, "delete_product", new() { ["productId"] = id });

        deleted.Id.ShouldBe(id);
        rest.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        search.Items.ShouldBeEmpty();
        again.ShouldContain(id.ToString());
    }

    /// <summary>What the description promises after a delete: recipes keep an Unknown line without nutrition, diary meals keep the product.</summary>
    [Fact]
    public async Task DeleteProduct_RecipesShowUnknownAndDiaryKeepsIt()
    {
        var me = $"pe-refs-{_tag}";
        var rest = _mcp.Rest(me);
        var (product, recipe) = ($"Paneer {_tag}", $"Curry {_tag}");
        var import = new ImportDto(
            [new ImportProductDto(product, 300m, 18m, 4m, 22m, 0m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto(product, 200m, "g")])],
            [new ImportScheduleDto("2026-11-05", [new ImportMealDto("Lunch", recipe, 1m, null)])]);
        (await rest.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var productId = (await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = product })).Items.Single().Id;
        var recipeId = (await McpTestHost.CallAsync<CatalogPageDto<CatalogRecipeDto>>(client, "search_recipes", new() { ["query"] = recipe })).Items.Single().Id;
        var meal = (await McpTestHost.CallAsync<IReadOnlyList<DiaryMealDto>>(client, "get_meal_entries", Day("2026-11-05"))).Single();
        (await rest.PatchAsJsonAsync($"/api/v1/meals/{meal.Id}/override",
            new OverrideMealEntryRequest(null, [new ActualProductRequest(productId, 50m, "g")]), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        await McpTestHost.CallAsync<DeletedItemDto>(client, "delete_product", new() { ["productId"] = productId });
        var details = await McpTestHost.CallAsync<CatalogRecipeDto>(client, "get_recipe", new() { ["recipeId"] = recipeId });
        var diary = (await McpTestHost.CallAsync<IReadOnlyList<DiaryMealDto>>(client, "get_meal_entries", Day("2026-11-05"))).Single();

        details.Ingredients.ShouldHaveSingleItem().ProductName.ShouldBe("Unknown");
        details.NutritionPerServing.ShouldNotBeNull().Calories.ShouldBe(0m);
        diary.ActualProducts.ShouldBe([$"{product} 50 g"]);
    }

    /// <summary>A household adult may delete a shared product; a guest of the household may not, and it stays.</summary>
    [Fact]
    public async Task DeleteProduct_HouseholdAdultAllowedGuestRefused()
    {
        var (owner, adult, guest) = ($"pe-hh-owner-{_tag}", $"pe-hh-adult-{_tag}", $"pe-hh-guest-{_tag}");
        _mcp.Households.Join(owner, adult, guest);
        _mcp.Households.Contexts[guest] = _mcp.Households.Contexts[guest] with { Role = "Guest" };
        var byAdult = await CreateAsync(owner, $"Yoghurt {_tag}", 61m, "Household");
        var byGuest = await CreateAsync(owner, $"Cream {_tag}", 340m, "Household");
        await using var adultClient = await _mcp.ConnectAsync(adult, WriteScopes);
        await using var guestClient = await _mcp.ConnectAsync(guest, WriteScopes);

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(adultClient, "delete_product", new() { ["productId"] = byAdult });
        var refused = await McpTestHost.CallExpectingErrorAsync(guestClient, "delete_product", new() { ["productId"] = byGuest });

        deleted.Id.ShouldBe(byAdult);
        (await _mcp.Rest(owner).GetAsync($"/api/v1/products/{byAdult}", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        refused.ShouldContain("adult of its household");
        (await GetAsync(owner, byGuest)).Name.ShouldBe($"Cream {_tag}");
    }

    /// <summary>Another person cannot see, change or delete the caller's private product; it stays untouched.</summary>
    [Fact]
    public async Task EditTools_ForStranger_FailLikeMissingAndChangeNothing()
    {
        var (me, stranger) = ($"pe-me-{_tag}", $"pe-stranger-{_tag}");
        var id = await CreateAsync(me, $"Honey {_tag}", 304m, "Private");
        await using var client = await _mcp.ConnectAsync(stranger, WriteScopes);

        var update = await McpTestHost.CallExpectingErrorAsync(client, "update_product", Args(id, $"Stolen {_tag}"));
        var delete = await McpTestHost.CallExpectingErrorAsync(client, "delete_product", new() { ["productId"] = id });
        var missing = await McpTestHost.CallExpectingErrorAsync(client, "delete_product", new() { ["productId"] = Guid.CreateVersion7() });

        update.ShouldContain("not found", Case.Insensitive);
        delete.ShouldContain("not found", Case.Insensitive);
        missing.ShouldContain("not found", Case.Insensitive);
        (await GetAsync(me, id)).Name.ShouldBe($"Honey {_tag}");
    }

    /// <summary>A read-only token neither lists nor runs the edit tools.</summary>
    [Theory]
    [InlineData("update_product")]
    [InlineData("delete_product")]
    public async Task EditTools_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"pe-ro-{_tag}";
        var id = await CreateAsync(me, $"Salt {_tag}", 0m, "Private");
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, Args(id, $"Changed {_tag}"), cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await GetAsync(me, id)).Name.ShouldBe($"Salt {_tag}");
    }

    /// <summary>The assistant can run the edit tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task EditTools_InProcess_UpdateAndDelete()
    {
        var me = $"pe-inproc-{_tag}";
        var id = await CreateAsync(me, $"Lentils {_tag}", 116m, "Private");
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var update = await invoker.InvokeAsync(user, "update_product", Json(Args(id, $"Red lentils {_tag}")), TestContext.Current.CancellationToken);
        var renamed = await GetAsync(me, id);
        var delete = await invoker.InvokeAsync(user, "delete_product", Json(new() { ["productId"] = id }), TestContext.Current.CancellationToken);
        var gone = await _mcp.Rest(me).GetAsync($"/api/v1/products/{id}", TestContext.Current.CancellationToken);

        update.IsError.ShouldNotBe(true);
        renamed.Name.ShouldBe($"Red lentils {_tag}");
        delete.IsError.ShouldNotBe(true);
        gone.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Args(Guid id, string name, string? visibility = null)
    {
        var args = new Dictionary<string, object?> { ["productId"] = id, ["name"] = name, ["unit"] = "g" };
        if (visibility is not null)
            args["visibility"] = visibility;
        return args;
    }

    private static Dictionary<string, object?> Day(string date) => new() { ["from"] = date, ["to"] = date };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));

    private async Task<Guid> CreateAsync(string subject, string name, decimal calories, string visibility)
    {
        var response = await _mcp.Rest(subject).PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest(name, calories, null, null, null, null, "g", null, null, visibility), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return Guid.Parse((await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"'));
    }

    private async Task<ProductDto> GetAsync(string subject, Guid id)
        => (await _mcp.Rest(subject).GetFromJsonAsync<ProductDto>($"/api/v1/products/{id}", TestContext.Current.CancellationToken))!;
}
