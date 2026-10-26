namespace DietPlanner.IntegrationTests.Mcp;

using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using ModelContextProtocol;

/// <summary>Integration tests for <c>create_product</c>: REST-equivalent creation plus durable operation replay on PostgreSQL.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpCreateProductTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>A created product keeps unknown nutrition unknown and defaults to household visibility.</summary>
    [Fact]
    public async Task CreateProduct_KeepsUnknownNutritionNull()
    {
        await using var client = await _mcp.ConnectAsync($"cp-null-{_tag}", WriteScopes);

        var created = await CreateAsync(client, Key(), $"Kefir {_tag}", calories: 41m);
        var product = await McpTestHost.CallAsync<CatalogProductDto>(client, "get_product", new() { ["productId"] = created.ProductId });

        created.Visibility.ShouldBe("Household");
        product.CaloriesPer100g.ShouldBe(41m);
        product.ProteinPer100g.ShouldBeNull();
        product.FiberPer100g.ShouldBeNull();
    }

    /// <summary>Retrying with the same key returns the original product, even from a freshly started host.</summary>
    [Fact]
    public async Task CreateProduct_RetriedWithSameKey_ReturnsOriginalAcrossRestart()
    {
        var (subject, key) = ($"cp-replay-{_tag}", Key());
        await using var client = await _mcp.ConnectAsync(subject, WriteScopes);
        var first = await CreateAsync(client, key, $"Tahini {_tag}");
        var second = await CreateAsync(client, key, $"Tahini {_tag}");

        using var restarted = new McpTestHost(db.ConnectionString);
        await using var fresh = await restarted.ConnectAsync(subject, WriteScopes);
        var third = await CreateAsync(fresh, key, $"Tahini {_tag}");

        second.ProductId.ShouldBe(first.ProductId);
        third.ProductId.ShouldBe(first.ProductId);
        (await CountAsync(fresh, $"Tahini {_tag}")).ShouldBe(1);
    }

    /// <summary>Concurrent calls with one key create one product and all return it.</summary>
    [Fact]
    public async Task CreateProduct_ConcurrentWithSameKey_CreatesOnce()
    {
        var (subject, key) = ($"cp-race-{_tag}", Key());
        var clients = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _mcp.ConnectAsync(subject, WriteScopes)));
        try
        {
            var results = await Task.WhenAll(clients.Select(c => CreateAsync(c, key, $"Miso {_tag}")));

            results.Select(r => r.ProductId).Distinct().ShouldHaveSingleItem();
            (await CountAsync(clients[0], $"Miso {_tag}")).ShouldBe(1);
        }
        finally
        {
            foreach (var c in clients)
                await c.DisposeAsync();
        }
    }

    /// <summary>The same key with different fields is a conflict and creates nothing.</summary>
    [Fact]
    public async Task CreateProduct_SameKeyDifferentFields_Conflicts()
    {
        var key = Key();
        await using var client = await _mcp.ConnectAsync($"cp-conflict-{_tag}", WriteScopes);
        await CreateAsync(client, key, $"Feta {_tag}");

        var error = await McpTestHost.CallExpectingErrorAsync(client, "create_product", Args(key, $"Halloumi {_tag}"));

        error.ShouldContain("already used for a different");
        (await CountAsync(client, $"Halloumi {_tag}")).ShouldBe(0);
    }

    /// <summary>A failed create rolls its receipt back too, so the key stays free for a corrected call.</summary>
    [Fact]
    public async Task CreateProduct_WhenCreateFails_RollsBackReceipt()
    {
        var key = Key();
        await using var client = await _mcp.ConnectAsync($"cp-rollback-{_tag}", WriteScopes);
        await CreateAsync(client, Key(), $"Taken {_tag}");

        var duplicate = await McpTestHost.CallExpectingErrorAsync(client, "create_product", Args(key, $"Taken {_tag}"));
        var corrected = await McpTestHost.CallExpectingErrorAsync(client, "create_product", Args(key, $"Taken {_tag}"));
        var different = await CreateAsync(client, key, $"Free {_tag}");

        duplicate.ShouldContain("already exists");
        corrected.ShouldContain("already exists");
        different.Name.ShouldBe($"Free {_tag}");
    }

    /// <summary>Read-only tokens cannot create, and malformed keys are rejected.</summary>
    [Fact]
    public async Task CreateProduct_WithoutWriteScopeOrValidKey_Fails()
    {
        await using var reader = await _mcp.ConnectAsync($"cp-reader-{_tag}", DietPlannerMcp.ReadScope);
        await using var writer = await _mcp.ConnectAsync($"cp-writer-{_tag}", WriteScopes);

        var forbidden = await Should.ThrowAsync<McpProtocolException>(
            () => reader.CallToolAsync("create_product", Args(Key(), $"Nope {_tag}"), cancellationToken: TestContext.Current.CancellationToken).AsTask());
        var badKey = await McpTestHost.CallExpectingErrorAsync(writer, "create_product", Args("short", $"Nope {_tag}"));

        forbidden.Message.ShouldContain("forbidden", Case.Insensitive);
        badKey.ShouldContain("OperationKey");
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static string Key() => Guid.CreateVersion7().ToString();

    private static Dictionary<string, object?> Args(string key, string name, decimal? calories = null)
        => new() { ["operationKey"] = key, ["name"] = name, ["unit"] = "g", ["caloriesPer100g"] = calories };

    private static Task<CreatedProductDto> CreateAsync(ModelContextProtocol.Client.McpClient client, string key, string name, decimal? calories = null)
        => McpTestHost.CallAsync<CreatedProductDto>(client, "create_product", Args(key, name, calories));

    private static async Task<int> CountAsync(ModelContextProtocol.Client.McpClient client, string name)
        => (await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = name }))
            .Items.Count(p => p.Name == name);
}
