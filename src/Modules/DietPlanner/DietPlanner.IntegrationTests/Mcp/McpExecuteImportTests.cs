namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Api.Mcp;
using DietPlanner.Application.Commands.ExecutePreviewedImport;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.IntegrationTests.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Client;

/// <summary>Integration tests for <c>execute_plan_import</c>: atomic monthly import, durable replay, stale previews and grants on PostgreSQL.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpExecuteImportTests(DatabaseFixture db) : IDisposable
{
    private static readonly string[] SixSlots = ["Breakfast", "Second breakfast", "Lunch", "Snack", "Dinner", "Supper"];
    private static readonly string ImportScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope} {DietPlannerMcp.ImportScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>A previewed 31-day plan with new products and recipes commits as 186 planned meals the app shows.</summary>
    [Fact]
    public async Task ExecutePlanImport_FullMonth_CommitsEverythingAsPlannedMeals()
    {
        var me = $"ex-month-{_tag}";
        await SetScheduleAsync(_mcp, me);
        await using var client = await _mcp.ConnectAsync(me, ImportScopes);
        var plan = MonthPlan();

        var result = await ExecuteAsync(client, plan, await PreviewTokenAsync(client, plan), Key());
        var meals = await RestMealsAsync(_mcp, me);

        result.Replayed.ShouldBeFalse();
        result.Stats.ShouldBe(new ImportStatsDto(2, 0, 6, 0, 186));
        meals.Count.ShouldBe(186);
        meals.ShouldAllBe(m => m.Status == "Planned");
    }

    /// <summary>A lost response is recovered with the same key, also after a restart that invalidates the token, without importing twice.</summary>
    [Fact]
    public async Task ExecutePlanImport_RetriedWithSameKey_ReplaysAcrossRestart()
    {
        var (me, key, plan) = ($"ex-replay-{_tag}", Key(), MonthPlan());
        await SetScheduleAsync(_mcp, me);
        await using var client = await _mcp.ConnectAsync(me, ImportScopes);
        var token = await PreviewTokenAsync(client, plan);
        var first = await ExecuteAsync(client, plan, token, key);
        var second = await ExecuteAsync(client, plan, token, key);

        using var restarted = new McpTestHost(db.ConnectionString);
        await using var fresh = await restarted.ConnectAsync(me, ImportScopes);
        var third = await ExecuteAsync(fresh, plan, token, key);

        second.ShouldBe(first with { Replayed = true });
        third.ShouldBe(first with { Replayed = true });
        (await RestMealsAsync(restarted, me)).Count.ShouldBe(186);
    }

    /// <summary>Concurrent executions with one key import once.</summary>
    [Fact]
    public async Task ExecutePlanImport_ConcurrentWithSameKey_ImportsOnce()
    {
        var (me, key, plan) = ($"ex-race-{_tag}", Key(), MonthPlan());
        await SetScheduleAsync(_mcp, me);
        var clients = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => _mcp.ConnectAsync(me, ImportScopes)));
        try
        {
            var token = await PreviewTokenAsync(clients[0], plan);
            var results = await Task.WhenAll(clients.Select(c => ExecuteAsync(c, plan, token, key)));

            results.Select(r => r.OperationId).Distinct().ShouldHaveSingleItem();
            results.Count(r => !r.Replayed).ShouldBe(1);
            (await RestMealsAsync(_mcp, me)).Count.ShouldBe(186);
        }
        finally
        {
            foreach (var c in clients)
                await c.DisposeAsync();
        }
    }

    /// <summary>The same key with a different plan is a conflict and writes nothing.</summary>
    [Fact]
    public async Task ExecutePlanImport_SameKeyDifferentPlan_Conflicts()
    {
        var (me, key, plan) = ($"ex-conflict-{_tag}", Key(), MonthPlan());
        await SetScheduleAsync(_mcp, me);
        await using var client = await _mcp.ConnectAsync(me, ImportScopes);
        await ExecuteAsync(client, plan, await PreviewTokenAsync(client, plan), key);
        var other = plan with { Schedule = [new ImportScheduleDto("2026-11-02", [new ImportMealDto("Lunch", $"Lunch bowl {_tag}", 1m, null)])] };

        var error = await McpTestHost.CallExpectingErrorAsync(client, "execute_plan_import", Args(other, await PreviewTokenAsync(client, other), key));

        error.ShouldContain("already used for a different");
        (await RestMealsAsync(_mcp, me, "2026-11-01", "2026-11-30")).ShouldBeEmpty();
    }

    /// <summary>A slot filled after the preview is caught at execution: nothing is written, and the occupied slot is reported.</summary>
    [Fact]
    public async Task ExecutePlanImport_AfterDataChangedSincePreview_RequiresNewPreview()
    {
        var (me, plan) = ($"ex-stale-{_tag}", MonthPlan());
        await SetScheduleAsync(_mcp, me);
        await using var client = await _mcp.ConnectAsync(me, ImportScopes);
        var token = await PreviewTokenAsync(client, plan);
        (await _mcp.Rest(me).PostAsJsonAsync("/api/v1/meals/import", new ImportDto([], [],
            [new ImportScheduleDto("2026-10-15", [new ImportMealDto("Dinner", $"Dinner bowl {_tag}", 1m, null)])]) with
        {
            Products = plan.Products,
            Recipes = plan.Recipes,
        }, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var error = await McpTestHost.CallExpectingErrorAsync(client, "execute_plan_import", Args(plan, token, Key()));

        error.ShouldContain("already has a planned meal");
        (await RestMealsAsync(_mcp, me)).Count.ShouldBe(1);
    }

    /// <summary>A failure half way through writing rolls back products, recipes, meals and the receipt, so the same key works afterwards.</summary>
    [Fact]
    public async Task ExecutePlanImport_WhenWritingFailsHalfway_CommitsNothing()
    {
        var (me, key, plan) = ($"ex-fail-{_tag}", Key(), MonthPlan());
        await SetScheduleAsync(_mcp, me);
        using (var failing = new McpTestHost(db.ConnectionString, FailingMealEntryRepository.Install))
        {
            await using var broken = await failing.ConnectAsync(me, ImportScopes);
            var token = await PreviewTokenAsync(broken, plan);
            await McpTestHost.CallExpectingErrorAsync(broken, "execute_plan_import", Args(plan, token, key));
        }

        await using var client = await _mcp.ConnectAsync(me, ImportScopes);
        var products = await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = _tag });
        var recipes = await McpTestHost.CallAsync<CatalogPageDto<CatalogRecipeDto>>(client, "search_recipes", new() { ["query"] = _tag });
        var mealsAfterFailure = await RestMealsAsync(_mcp, me);
        var retried = await ExecuteAsync(client, plan, await PreviewTokenAsync(client, plan), key);

        products.TotalCount.ShouldBe(0);
        recipes.TotalCount.ShouldBe(0);
        mealsAfterFailure.ShouldBeEmpty();
        retried.Replayed.ShouldBeFalse();
        retried.Stats.MealEntriesCreated.ShouldBe(186);
    }

    /// <summary>A catalog change after the preview (same plan, now reusing instead of creating) requires a new preview.</summary>
    [Fact]
    public async Task ExecutePlanImport_AfterCatalogChangedSincePreview_RequiresNewPreview()
    {
        var (me, plan) = ($"ex-catalog-{_tag}", MonthPlan());
        await SetScheduleAsync(_mcp, me);
        await using var client = await _mcp.ConnectAsync(me, ImportScopes);
        var token = await PreviewTokenAsync(client, plan);
        await McpTestHost.CallAsync<CreatedProductDto>(client, "create_product", new()
        {
            ["operationKey"] = Key(),
            ["name"] = $"Oats {_tag}",
            ["unit"] = "g",
        });

        var error = await McpTestHost.CallExpectingErrorAsync(client, "execute_plan_import", Args(plan, token, Key()));

        error.ShouldContain("changed since the preview");
        (await RestMealsAsync(_mcp, me)).ShouldBeEmpty();
    }

    /// <summary>Someone else's preview token cannot be used.</summary>
    [Fact]
    public async Task ExecutePlanImport_WithAnotherUsersToken_Fails()
    {
        var (owner, intruder, plan) = ($"ex-owner-{_tag}", $"ex-intruder-{_tag}", MonthPlan());
        await SetScheduleAsync(_mcp, owner);
        await SetScheduleAsync(_mcp, intruder);
        await using var ownerClient = await _mcp.ConnectAsync(owner, ImportScopes);
        await using var intruderClient = await _mcp.ConnectAsync(intruder, ImportScopes);
        var token = await PreviewTokenAsync(ownerClient, plan);

        var error = await McpTestHost.CallExpectingErrorAsync(intruderClient, "execute_plan_import", Args(plan, token, Key()));

        error.ShouldContain("someone else");
        (await RestMealsAsync(_mcp, intruder)).ShouldBeEmpty();
    }

    /// <summary>Executing needs both the write and the import grant.</summary>
    [Theory]
    [InlineData("mcp:diet:read")]
    [InlineData("mcp:diet:read mcp:diet:write")]
    [InlineData("mcp:diet:read mcp:diet:import")]
    public async Task ExecutePlanImport_WithoutWriteAndImportGrants_IsForbidden(string scopes)
    {
        await using var client = await _mcp.ConnectAsync($"ex-grant-{_tag}", scopes);

        var forbidden = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync(
            "execute_plan_import", Args(MonthPlan(), "token", Key()), cancellationToken: TestContext.Current.CancellationToken).AsTask());

        forbidden.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static string Key() => Guid.CreateVersion7().ToString();

    private ImportDto MonthPlan()
        => new(
            [new ImportProductDto($"Oats {_tag}", 380m, 13m, 60m, 7m, 10m, "g", null, null),
             new ImportProductDto($"Milk {_tag}", 64m, 3.3m, 4.8m, 3.6m, null, "ml", 1.03m, null)],
            [.. SixSlots.Select(s => new ImportRecipeDto($"{s} bowl {_tag}", null, null, 1, null,
                [new ImportIngredientDto($"Oats {_tag}", 50m, "g"), new ImportIngredientDto($"Milk {_tag}", 200m, "ml")]))],
            [.. Enumerable.Range(1, 31).Select(d => new ImportScheduleDto(
                $"2026-10-{d:00}", [.. SixSlots.Select(s => new ImportMealDto(s, $"{s} bowl {_tag}", 1m, null))]))]);

    private static Dictionary<string, object?> Args(ImportDto plan, string token, string key)
        => new() { ["plan"] = plan, ["previewToken"] = token, ["operationKey"] = key };

    private static async Task<string> PreviewTokenAsync(McpClient client, ImportDto plan)
        => (await McpTestHost.CallAsync<ImportPreviewResultDto>(client, "preview_plan_import", new() { ["plan"] = plan })).PreviewToken
            ?? throw new InvalidOperationException("The plan did not preview as executable.");

    private static Task<ExecutedImportDto> ExecuteAsync(McpClient client, ImportDto plan, string token, string key)
        => McpTestHost.CallAsync<ExecutedImportDto>(client, "execute_plan_import", Args(plan, token, key));

    private static async Task SetScheduleAsync(McpTestHost host, string subject)
        => (await host.Rest(subject).PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([.. SixSlots.Select((s, i) => new MealSlotRequest(null, s, $"{7 + i:00}:00"))]),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

    private static async Task<IReadOnlyList<MealEntryDto>> RestMealsAsync(McpTestHost host, string subject, string from = "2026-10-01", string to = "2026-10-31")
        => (await host.Rest(subject).GetFromJsonAsync<IReadOnlyList<MealEntryDto>>(
            $"/api/v1/meals?from={from}&to={to}", TestContext.Current.CancellationToken))!;
}
