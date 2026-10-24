namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Api.Mcp;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.IntegrationTests.Infrastructure;
using ModelContextProtocol.Client;

/// <summary>Integration tests for <c>preview_plan_import</c>: a whole month previews without writing, invalid plans get paths and no token.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpPreviewImportTests(DatabaseFixture db) : IDisposable
{
    private static readonly string[] SixSlots = ["Breakfast", "Second breakfast", "Lunch", "Snack", "Dinner", "Supper"];

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>A 31-day, 186-meal plan with new products and recipes previews in full, issues a token and writes nothing.</summary>
    [Fact]
    public async Task PreviewPlanImport_FullMonth_DescribesEverythingAndWritesNothing()
    {
        var me = $"pv-month-{_tag}";
        await SetScheduleAsync(me, SixSlots);
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var result = await PreviewAsync(client, MonthPlan());
        var products = await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = _tag });
        var meals = await McpTestHost.CallAsync<IReadOnlyList<DiaryMealDto>>(client, "get_meal_entries", new() { ["from"] = "2026-10-01", ["to"] = "2026-10-31" });

        result.Preview.CanExecute.ShouldBeTrue();
        result.Preview.Counts.ShouldNotBeNull().ShouldSatisfyAllConditions(
            c => c.ProductsToCreate.ShouldBe(2),
            c => c.RecipesToCreate.ShouldBe(6),
            c => c.MealEntriesToCreate.ShouldBe(186));
        result.Preview.Days.Count.ShouldBe(31);
        result.Preview.Days[0].Meals.Select(m => m.Slot).ShouldBe(SixSlots);
        result.Preview.ProductsWithUnknownNutrition.ShouldBe([$"Milk {_tag}"]);
        result.PreviewToken.ShouldNotBeNullOrWhiteSpace();
        products.TotalCount.ShouldBe(0);
        meals.ShouldBeEmpty();
    }

    /// <summary>An invalid plan returns indexed problems and no token.</summary>
    [Fact]
    public async Task PreviewPlanImport_InvalidPlan_ReturnsPathsAndNoToken()
    {
        var me = $"pv-invalid-{_tag}";
        await SetScheduleAsync(me, "Lunch");
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var plan = new ImportDto([], [], [
            new ImportScheduleDto("2026-10-01", [new ImportMealDto("Lunch", $"Missing {_tag}", 1m, null)]),
            new ImportScheduleDto("2026-10-02", [new ImportMealDto("Brunch", $"Missing {_tag}", 1m, null)]),
        ]);

        var result = await PreviewAsync(client, plan);

        result.Preview.CanExecute.ShouldBeFalse();
        result.PreviewToken.ShouldBeNull();
        result.Preview.Issues.ShouldContain(i => i.Severity == "error" && i.Path == "schedule[0].meals[0].recipe");
        result.Preview.Issues.ShouldContain(i => i.Severity == "error" && i.Path == "schedule[1].meals[0].type");
    }

    /// <summary>The fingerprint changes when the plan or the options change, and stays put for an identical request.</summary>
    [Fact]
    public async Task PreviewPlanImport_Fingerprint_TracksPayloadAndOptions()
    {
        var me = $"pv-fp-{_tag}";
        await SetScheduleAsync(me, SixSlots);
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var plan = MonthPlan();
        var changed = plan with { Schedule = [.. plan.Schedule!.Take(30)] };

        var first = await PreviewAsync(client, plan);
        var again = await PreviewAsync(client, plan);
        var shorter = await PreviewAsync(client, changed);
        var appending = await PreviewAsync(client, plan, appendToOccupiedSlots: true);

        again.Preview.Fingerprint.ShouldBe(first.Preview.Fingerprint);
        shorter.Preview.Fingerprint.ShouldNotBe(first.Preview.Fingerprint);
        appending.Preview.Fingerprint.ShouldNotBe(first.Preview.Fingerprint);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private ImportDto MonthPlan()
        => new(
            [new ImportProductDto($"Oats {_tag}", 380m, 13m, 60m, 7m, 10m, "g", null, null),
             new ImportProductDto($"Milk {_tag}", 64m, null, 4.8m, 3.6m, null, "ml", 1.03m, null)],
            [.. SixSlots.Select(s => new ImportRecipeDto($"{s} bowl {_tag}", null, null, 1, null,
                [new ImportIngredientDto($"Oats {_tag}", 50m, "g"), new ImportIngredientDto($"Milk {_tag}", 200m, "ml")]))],
            [.. Enumerable.Range(1, 31).Select(d => new ImportScheduleDto(
                $"2026-10-{d:00}", [.. SixSlots.Select(s => new ImportMealDto(s, $"{s} bowl {_tag}", 1m, null))]))]);

    private async Task SetScheduleAsync(string subject, params string[] slots)
        => (await _mcp.Rest(subject).PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([.. slots.Select((s, i) => new MealSlotRequest(null, s, $"{7 + i:00}:00"))]),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

    private static Task<ImportPreviewResultDto> PreviewAsync(McpClient client, ImportDto plan, bool appendToOccupiedSlots = false)
        => McpTestHost.CallAsync<ImportPreviewResultDto>(client, "preview_plan_import", new()
        {
            ["plan"] = plan,
            ["appendToOccupiedSlots"] = appendToOccupiedSlots,
        });
}
