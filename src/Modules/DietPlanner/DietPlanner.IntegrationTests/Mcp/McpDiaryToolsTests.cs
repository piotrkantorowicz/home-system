namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using DietPlanner.Api.Mcp;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.GetNutritionSummary;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>Integration tests for the diary MCP tools: the caller's month matches REST, ranges are bounded and other people's diaries stay out.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpDiaryToolsTests(DatabaseFixture db) : IDisposable
{
    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>A full calendar month of planned and completed meals reads the same over MCP as over REST.</summary>
    [Fact]
    public async Task GetMealEntries_FullMonth_MatchesRest()
    {
        var me = $"diary-me-{_tag}";
        var rest = _mcp.Rest(me);
        await PlanMonthAsync(rest, $"Porridge {_tag}");
        var restMeals = await rest.GetFromJsonAsync<IReadOnlyList<MealEntryDto>>(
            "/api/v1/meals?from=2026-10-01&to=2026-10-31", TestContext.Current.CancellationToken);
        (await rest.PatchAsync($"/api/v1/meals/{restMeals![0].Id}/complete", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var meals = await McpTestHost.CallAsync<IReadOnlyList<DiaryMealDto>>(client, "get_meal_entries", Range("2026-10-01", "2026-10-31"));

        meals.Count.ShouldBe(31);
        meals.Select(m => m.Id).ShouldBe(restMeals.OrderBy(m => m.Date).Select(m => m.Id));
        meals[0].Status.ShouldBe("Done");
        meals.Skip(1).ShouldAllBe(m => m.Status == "Planned");
    }

    /// <summary>Daily totals over MCP equal the REST nutrition summary.</summary>
    [Fact]
    public async Task GetNutritionSummary_MatchesRest()
    {
        var me = $"diary-sum-{_tag}";
        var rest = _mcp.Rest(me);
        await PlanMonthAsync(rest, $"Stew {_tag}");
        var expected = await rest.GetFromJsonAsync<IReadOnlyList<DailyNutritionDto>>(
            "/api/v1/meals/nutrition-summary?from=2026-10-01&to=2026-10-31", TestContext.Current.CancellationToken);

        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var summary = await McpTestHost.CallAsync<IReadOnlyList<DailyNutritionDto>>(client, "get_nutrition_summary", Range("2026-10-01", "2026-10-31"));

        summary.ShouldBe(expected);
        summary.Count.ShouldBe(31);
    }

    /// <summary>Ranges longer than 31 days or reversed are rejected.</summary>
    [Theory]
    [InlineData("get_meal_entries", "2026-10-01", "2026-11-01", "at most 31")]
    [InlineData("get_nutrition_summary", "2026-10-01", "2026-11-01", "at most 31")]
    [InlineData("get_meal_entries", "2026-10-02", "2026-10-01", "before from")]
    public async Task DiaryTools_WithInvalidRange_Fail(string tool, string from, string to, string message)
    {
        await using var client = await _mcp.ConnectAsync($"diary-range-{_tag}", DietPlannerMcp.ReadScope);

        var error = await McpTestHost.CallExpectingErrorAsync(client, tool, Range(from, to));

        error.ShouldContain(message);
    }

    /// <summary>Another person's diary on the same dates is not visible.</summary>
    [Fact]
    public async Task GetMealEntries_DoesNotShowOtherPeoplesMeals()
    {
        await PlanMonthAsync(_mcp.Rest($"diary-other-{_tag}"), $"Other {_tag}");

        await using var client = await _mcp.ConnectAsync($"diary-empty-{_tag}", DietPlannerMcp.ReadScope);
        var meals = await McpTestHost.CallAsync<IReadOnlyList<DiaryMealDto>>(client, "get_meal_entries", Range("2026-10-01", "2026-10-31"));
        var summary = await McpTestHost.CallAsync<IReadOnlyList<DailyNutritionDto>>(client, "get_nutrition_summary", Range("2026-10-01", "2026-10-31"));

        meals.ShouldBeEmpty();
        summary.ShouldBeEmpty();
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Range(string from, string to) => new() { ["from"] = from, ["to"] = to };

    private static async Task PlanMonthAsync(HttpClient rest, string recipe)
    {
        var import = new ImportDto(
            [new ImportProductDto($"{recipe} base", 120m, 4m, 20m, 2m, 3m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto($"{recipe} base", 250m, "g")])],
            [.. Enumerable.Range(1, 31).Select(d => new ImportScheduleDto($"2026-10-{d:00}", [new ImportMealDto("Lunch", recipe, 1m, null)]))]);
        (await rest.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
    }
}
