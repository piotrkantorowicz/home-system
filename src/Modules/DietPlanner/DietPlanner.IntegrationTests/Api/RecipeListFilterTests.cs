namespace DietPlanner.IntegrationTests.Api;

using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.SearchRecipes;
using DietPlanner.IntegrationTests.Infrastructure;
using Shared.Abstractions.Core.Pagination;

/// <summary>#486 — recipe list: high-protein and under-15-minute filters apply before paging.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class RecipeListFilterTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatabaseFixture _db;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public RecipeListFilterTests(DatabaseFixture db) => _db = db;

    private HttpClient ClientFor(string user) => new DietPlannerWebApplicationFactory(_db.ConnectionString, user).CreateClient();

    private static async Task<Guid> ProductAsync(HttpClient client, string name, decimal? calories, decimal? protein, string? visibility = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/products", new CreateProductRequest(name, calories, protein, 10m, 10m, 1m, "g", null, null, visibility), Ct);
        response.EnsureSuccessStatusCode();
        return Guid.Parse((await response.Content.ReadAsStringAsync(Ct)).Trim('"'));
    }

    private static async Task RecipeAsync(HttpClient client, string name, int servings, int? prep, string? visibility = null, params Guid[] products)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeRequest(name, null, null, servings, prep, [.. products.Select(p => new RecipeIngredientRequest(p, 100m, "g"))], visibility), Ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PagedList<RecipeDto>> ListAsync(HttpClient client, string query)
        => (await client.GetFromJsonAsync<PagedList<RecipeDto>>($"/api/v1/recipes?{query}", Ct))!;

    private static List<string> Names(PagedList<RecipeDto> page, string tag) => [.. page.Items.Select(r => r.Name.Replace($"{tag}-", "")).Order()];

    /// <summary>High protein: 25 g or 30% of calories per serving, inclusive; unknown protein never matches; servings divide the total.</summary>
    [Fact]
    public async Task HighProtein_UsesPerServingThresholds_AndExcludesUnknown()
    {
        var tag = $"hp{Guid.NewGuid():N}";
        var me = ClientFor($"hp-me-{Guid.NewGuid():N}");
        var p25 = await ProductAsync(me, $"{tag}-p25", 500, 25);
        var p24 = await ProductAsync(me, $"{tag}-p24", 500, 24);
        var share30 = await ProductAsync(me, $"{tag}-s30", 200, 15);
        var share29 = await ProductAsync(me, $"{tag}-s29", 200, 14.5m);
        var big = await ProductAsync(me, $"{tag}-big", 800, 50);
        var unknown = await ProductAsync(me, $"{tag}-unk", 100, null);
        await RecipeAsync(me, $"{tag}-gram-edge", 1, 20, null, p25);
        await RecipeAsync(me, $"{tag}-gram-below", 1, 20, null, p24);
        await RecipeAsync(me, $"{tag}-share-edge", 1, 20, null, share30);
        await RecipeAsync(me, $"{tag}-share-below", 1, 20, null, share29);
        await RecipeAsync(me, $"{tag}-two-servings", 2, 20, null, big);
        await RecipeAsync(me, $"{tag}-null-protein", 1, 20, null, unknown);
        await RecipeAsync(me, $"{tag}-mixed-unknown", 1, 20, null, big, unknown);

        var page = await ListAsync(me, $"search={tag}&onlyHighProtein=true");

        Names(page, tag).ShouldBe(["gram-edge", "share-edge", "two-servings"]);
        page.TotalCount.ShouldBe(3);
    }

    /// <summary>Under 15 minutes means 14 matches, 15 and unknown do not.</summary>
    [Fact]
    public async Task Quick_IsStrictlyUnder15_AndSkipsUnknown()
    {
        var tag = $"qk{Guid.NewGuid():N}";
        var me = ClientFor($"qk-me-{Guid.NewGuid():N}");
        var product = await ProductAsync(me, $"{tag}-prod", 100, 10);
        await RecipeAsync(me, $"{tag}-m14", 1, 14, null, product);
        await RecipeAsync(me, $"{tag}-m15", 1, 15, null, product);
        await RecipeAsync(me, $"{tag}-m0", 1, 0, null, product);
        await RecipeAsync(me, $"{tag}-unknown", 1, null, null, product);

        Names(await ListAsync(me, $"search={tag}&onlyQuick=true"), tag).ShouldBe(["m0", "m14"]);
        (await ListAsync(me, $"search={tag}")).TotalCount.ShouldBe(4);
    }

    /// <summary>Filters combine, count the filtered set across pages, and never reveal another user's private recipes.</summary>
    [Fact]
    public async Task Filters_CombineAndPage_AndRespectVisibility()
    {
        var tag = $"cb{Guid.NewGuid():N}";
        var me = ClientFor($"cb-me-{Guid.NewGuid():N}");
        var other = ClientFor($"cb-other-{Guid.NewGuid():N}");
        var rich = await ProductAsync(me, $"{tag}-rich", 400, 40);
        var plain = await ProductAsync(me, $"{tag}-plain", 400, 5);
        var otherRich = await ProductAsync(other, $"{tag}-orich", 400, 40, "Public");
        foreach (var n in new[] { "a", "b", "c" })
            await RecipeAsync(me, $"{tag}-{n}", 1, 10, null, rich);
        await RecipeAsync(me, $"{tag}-slow", 1, 45, null, rich);
        await RecipeAsync(me, $"{tag}-plain", 1, 10, null, plain);
        await RecipeAsync(other, $"{tag}-secret", 1, 10, "Private", otherRich);

        var first = await ListAsync(me, $"search={tag}&onlyHighProtein=true&onlyQuick=true&pageSize=2&page=1");
        var second = await ListAsync(me, $"search={tag}&onlyHighProtein=true&onlyQuick=true&pageSize=2&page=2");

        first.TotalCount.ShouldBe(3);
        Names(first, tag).Concat(Names(second, tag)).Order().ShouldBe(["a", "b", "c"]);
        second.Items.Count.ShouldBe(1);
        (await ListAsync(me, $"search={tag}&onlyHighProtein=true&onlyMine=true")).TotalCount.ShouldBe(4);
    }
}
