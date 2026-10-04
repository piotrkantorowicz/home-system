namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.SearchProducts;
using DietPlanner.IntegrationTests.Infrastructure;
using Shared.Abstractions.Core.Pagination;

/// <summary>#485 — product list: incomplete-nutrition filter and whitelisted stable sorting, both applied before paging.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class ProductListFilterTests : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly DatabaseFixture _db;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public ProductListFilterTests(DatabaseFixture db) => _db = db;

    private readonly List<DietPlannerWebApplicationFactory> _factories = [];

    private HttpClient ClientFor(string user)
    {
        var factory = new DietPlannerWebApplicationFactory(_db.ConnectionString, user);
        _factories.Add(factory);
        return factory.CreateClient();
    }

    /// <summary>Disposes every host the test booted so they do not pile up across the suite.</summary>
    public void Dispose()
    {
        foreach (var factory in _factories)
            factory.Dispose();
    }

    private static async Task AddAsync(
        HttpClient client, string name, decimal? calories, decimal? protein, decimal? carbs, decimal? fat, decimal? fiber, string? visibility = null)
        => (await client.PostAsJsonAsync(
            "/api/v1/products", new CreateProductRequest(name, calories, protein, carbs, fat, fiber, "g", null, null, visibility), Ct))
            .EnsureSuccessStatusCode();

    private static async Task<PagedList<ProductDto>> ListAsync(HttpClient client, string query)
        => (await client.GetFromJsonAsync<PagedList<ProductDto>>($"/api/v1/products?{query}", Ct))!;

    /// <summary>Incomplete means calories, protein, carbs or fat unknown; fibre is optional. It combines with search and the owner filter, and the total follows.</summary>
    [Fact]
    public async Task Incomplete_FiltersBeforePaging_AndCombinesWithSearchAndMine()
    {
        var tag = $"inc{Guid.NewGuid():N}";
        var me = ClientFor($"inc-me-{Guid.NewGuid():N}");
        var other = ClientFor($"inc-other-{Guid.NewGuid():N}");
        await AddAsync(me, $"{tag}-full", 100, 10, 10, 10, 1);
        await AddAsync(me, $"{tag}-nofiber", 100, 10, 10, 10, null);
        await AddAsync(me, $"{tag}-noprotein", 100, null, 10, 10, 1);
        await AddAsync(me, $"{tag}-nocal", null, 10, 10, 10, 1);
        await AddAsync(me, $"{tag}-empty", null, null, null, null, null);
        await AddAsync(other, $"{tag}-other-incomplete", null, null, null, null, null, "Public");
        await AddAsync(other, $"{tag}-other-secret", null, null, null, null, null, "Private");

        var all = await ListAsync(me, $"search={tag}&onlyIncomplete=true&pageSize=2");
        all.TotalCount.ShouldBe(4);
        all.Items.Count.ShouldBe(2);

        var names = (await ListAsync(me, $"search={tag}&onlyIncomplete=true")).Items.Select(p => p.Name).Order().ToList();
        names.ShouldBe([$"{tag}-empty", $"{tag}-nocal", $"{tag}-noprotein", $"{tag}-other-incomplete"]);

        (await ListAsync(me, $"search={tag}&onlyIncomplete=true&onlyMine=true")).TotalCount.ShouldBe(3);
        (await ListAsync(me, $"search={tag}-nocal&onlyIncomplete=true")).TotalCount.ShouldBe(1);
        (await ListAsync(me, $"search={tag}&onlyIncomplete=false")).TotalCount.ShouldBe(6);
    }

    /// <summary>Sorting by a nutrition column puts unknown values last in both directions and pages stably through ties.</summary>
    [Fact]
    public async Task Sort_PutsUnknownLast_AndIsStableAcrossPages()
    {
        var tag = $"srt{Guid.NewGuid():N}";
        var me = ClientFor($"srt-me-{Guid.NewGuid():N}");
        await AddAsync(me, $"{tag}-b", 100, 20, 1, 1, 1);
        await AddAsync(me, $"{tag}-a", 100, 20, 1, 1, 1);
        await AddAsync(me, $"{tag}-c", 100, 5, 1, 1, 1);
        await AddAsync(me, $"{tag}-d", 100, null, 1, 1, 1);
        await AddAsync(me, $"{tag}-e", 100, 40, 1, 1, 1);

        async Task<List<string>> NamesAsync(string sort, bool desc)
        {
            var names = new List<string>();
            for (var page = 1; page <= 3; page++)
                names.AddRange((await ListAsync(me, $"search={tag}&sortBy={sort}&sortDescending={desc}&page={page}&pageSize=2")).Items.Select(p => p.Name));
            return names;
        }

        (await NamesAsync("protein", false)).ShouldBe([$"{tag}-c", $"{tag}-a", $"{tag}-b", $"{tag}-e", $"{tag}-d"]);
        (await NamesAsync("protein", true)).ShouldBe([$"{tag}-e", $"{tag}-a", $"{tag}-b", $"{tag}-c", $"{tag}-d"]);
        (await NamesAsync("name", true)).ShouldBe([$"{tag}-e", $"{tag}-d", $"{tag}-c", $"{tag}-b", $"{tag}-a"]);
        (await NamesAsync("NAME", false)).First().ShouldBe($"{tag}-a");
    }

    /// <summary>Only whitelisted columns sort.</summary>
    [Fact]
    public async Task Sort_RejectsUnknownColumn()
    {
        var response = await ClientFor($"srt-bad-{Guid.NewGuid():N}").GetAsync("/api/v1/products?sortBy=createdByUserId", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
