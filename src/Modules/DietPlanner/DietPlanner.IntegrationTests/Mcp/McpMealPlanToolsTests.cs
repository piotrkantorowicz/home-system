namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>plan_meal</c>, <c>update_planned_meal</c> and <c>delete_planned_meal</c>:
/// REST-equivalent results, operation-key replay and access checks through real dispatchers and PostgreSQL.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpMealPlanToolsTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>A meal planned over MCP reads back the same through REST.</summary>
    [Fact]
    public async Task PlanMeal_ResultMatchesRest()
    {
        var me = $"mp-plan-{_tag}";
        var (lunch, recipe) = await SetUpAsync(me);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var planned = await McpTestHost.CallAsync<DiaryMealDto>(client, "plan_meal",
            Plan(Key(), "2026-11-10", lunch, recipe, 1.5m, notes: "after gym", mealTime: "13:30"));
        var rest = await RestDayAsync(me, "2026-11-10");

        planned.ShouldBe(DiaryMealDto.From(rest.Single()), new DiaryComparer());
        planned.ShouldSatisfyAllConditions(
            m => m.SlotId.ShouldBe(lunch),
            m => m.Servings.ShouldBe(1.5m),
            m => m.MealTime.ShouldBe(new TimeOnly(13, 30)),
            m => m.Status.ShouldBe("Planned"));
    }

    /// <summary>A retry with the same key returns the original meal; the same key with other fields conflicts.</summary>
    [Fact]
    public async Task PlanMeal_Replay_ReturnsSameMealAndRejectsChangedFields()
    {
        var me = $"mp-replay-{_tag}";
        var (lunch, recipe) = await SetUpAsync(me);
        var key = Key();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var first = await McpTestHost.CallAsync<DiaryMealDto>(client, "plan_meal", Plan(key, "2026-11-11", lunch, recipe, 1m));
        var second = await McpTestHost.CallAsync<DiaryMealDto>(client, "plan_meal", Plan(key, "2026-11-11", lunch, recipe, 1m));
        var changed = await McpTestHost.CallExpectingErrorAsync(client, "plan_meal", Plan(key, "2026-11-11", lunch, recipe, 2m));

        second.Id.ShouldBe(first.Id);
        changed.ShouldContain("already used for a different");
        (await RestDayAsync(me, "2026-11-11")).ShouldHaveSingleItem();
    }

    /// <summary>An update replaces the fields, keeps whether the meal was eaten, and matches REST.</summary>
    [Fact]
    public async Task UpdatePlannedMeal_ReplacesFieldsKeepsStatus()
    {
        var me = $"mp-update-{_tag}";
        var (lunch, recipe) = await SetUpAsync(me);
        var meal = (await RestDayAsync(me, "2026-11-05")).Single();
        (await _mcp.Rest(me).PatchAsync($"/api/v1/meals/{meal.Id}/complete", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var updated = await McpTestHost.CallAsync<DiaryMealDto>(client, "update_planned_meal", new()
        {
            ["mealId"] = meal.Id,
            ["date"] = "2026-11-06",
            ["slotId"] = lunch,
            ["recipeId"] = recipe,
            ["servings"] = 2m,
        });
        var rest = await RestDayAsync(me, "2026-11-06");

        updated.ShouldBe(DiaryMealDto.From(rest.Single()), new DiaryComparer());
        updated.ShouldSatisfyAllConditions(
            m => m.Id.ShouldBe(meal.Id),
            m => m.Servings.ShouldBe(2m),
            m => m.Status.ShouldBe("Done"));
        (await RestDayAsync(me, "2026-11-05")).ShouldBeEmpty();
    }

    /// <summary>Zero servings, an unknown slot or a recipe the caller cannot see fail and change nothing.</summary>
    [Fact]
    public async Task UpdatePlannedMeal_WithInvalidInput_FailsWithoutChanging()
    {
        var (me, stranger) = ($"mp-invalid-{_tag}", $"mp-invalid-other-{_tag}");
        var (lunch, recipe) = await SetUpAsync(me);
        var (_, hidden) = await SetUpAsync(stranger, "Hidden");
        var meal = (await RestDayAsync(me, "2026-11-05")).Single();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var servings = await McpTestHost.CallExpectingErrorAsync(client, "update_planned_meal", Update(meal.Id, lunch, recipe, 0m));
        var slot = await McpTestHost.CallExpectingErrorAsync(client, "update_planned_meal", Update(meal.Id, Guid.CreateVersion7(), recipe, 1m));
        var foreign = await McpTestHost.CallExpectingErrorAsync(client, "update_planned_meal", Update(meal.Id, lunch, hidden, 1m));

        servings.ShouldContain("Servings");
        slot.ShouldContain("not found", Case.Insensitive);
        foreign.ShouldContain("not found", Case.Insensitive);
        (await RestDayAsync(me, "2026-11-05")).Single().ShouldBe(meal, new MealComparer());
    }

    /// <summary>Delete removes the meal as REST does, eaten or not; a repeat reports not found.</summary>
    [Fact]
    public async Task DeletePlannedMeal_RemovesItLikeRest()
    {
        var me = $"mp-delete-{_tag}";
        await SetUpAsync(me);
        var meal = (await RestDayAsync(me, "2026-11-05")).Single();
        (await _mcp.Rest(me).PatchAsync($"/api/v1/meals/{meal.Id}/complete", null, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(client, "delete_planned_meal", new() { ["mealId"] = meal.Id });
        var again = await McpTestHost.CallExpectingErrorAsync(client, "delete_planned_meal", new() { ["mealId"] = meal.Id });

        deleted.Id.ShouldBe(meal.Id);
        (await RestDayAsync(me, "2026-11-05")).ShouldBeEmpty();
        again.ShouldContain("not found", Case.Insensitive);
    }

    /// <summary>Someone outside the household cannot change, delete or plan with the caller's private data.</summary>
    [Fact]
    public async Task MealTools_ForStranger_FailLikeMissingAndChangeNothing()
    {
        var (me, stranger) = ($"mp-me-{_tag}", $"mp-stranger-{_tag}");
        var (_, recipe) = await SetUpAsync(me);
        var (theirLunch, theirRecipe) = await SetUpAsync(stranger, "Own");
        var meal = (await RestDayAsync(me, "2026-11-05")).Single();
        await using var client = await _mcp.ConnectAsync(stranger, WriteScopes);

        var update = await McpTestHost.CallExpectingErrorAsync(client, "update_planned_meal", Update(meal.Id, theirLunch, theirRecipe, 1m));
        var delete = await McpTestHost.CallExpectingErrorAsync(client, "delete_planned_meal", new() { ["mealId"] = meal.Id });
        var plan = await McpTestHost.CallExpectingErrorAsync(client, "plan_meal", Plan(Key(), "2026-11-12", theirLunch, recipe, 1m));

        update.ShouldContain("not found", Case.Insensitive);
        delete.ShouldContain("not found", Case.Insensitive);
        plan.ShouldContain("not found", Case.Insensitive);
        (await RestDayAsync(me, "2026-11-05")).Single().ShouldBe(meal, new MealComparer());
        (await RestDayAsync(stranger, "2026-11-12")).ShouldBeEmpty();
    }

    /// <summary>A read-only token neither lists nor runs the meal tools.</summary>
    [Theory]
    [InlineData("plan_meal")]
    [InlineData("update_planned_meal")]
    [InlineData("delete_planned_meal")]
    public async Task MealTools_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"mp-ro-{_tag}";
        var (lunch, recipe) = await SetUpAsync(me);
        var meal = (await RestDayAsync(me, "2026-11-05")).Single();
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var args = Update(meal.Id, lunch, recipe, 3m);
        args["operationKey"] = Key();

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, args, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await RestDayAsync(me, "2026-11-05")).Single().ShouldBe(meal, new MealComparer());
    }

    /// <summary>The assistant can run the meal tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task MealTools_InProcess_PlanUpdateDelete()
    {
        var me = $"mp-inproc-{_tag}";
        var (lunch, recipe) = await SetUpAsync(me);
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var plan = await invoker.InvokeAsync(user, "plan_meal", Json(Plan(Key(), "2026-11-13", lunch, recipe, 1m)), TestContext.Current.CancellationToken);
        var meal = (await RestDayAsync(me, "2026-11-13")).Single();
        var update = await invoker.InvokeAsync(user, "update_planned_meal", Json(Update(meal.Id, lunch, recipe, 2m, "2026-11-13")), TestContext.Current.CancellationToken);
        var servings = (await RestDayAsync(me, "2026-11-13")).Single().Servings;
        var delete = await invoker.InvokeAsync(user, "delete_planned_meal", Json(new() { ["mealId"] = meal.Id }), TestContext.Current.CancellationToken);

        plan.IsError.ShouldNotBe(true);
        update.IsError.ShouldNotBe(true);
        servings.ShouldBe(2m);
        delete.IsError.ShouldNotBe(true);
        (await RestDayAsync(me, "2026-11-13")).ShouldBeEmpty();
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static string Key() => Guid.CreateVersion7().ToString();

    private static Dictionary<string, object?> Plan(string key, string date, Guid slot, Guid recipe, decimal servings,
        string? notes = null, string? mealTime = null)
        => new()
        {
            ["operationKey"] = key,
            ["date"] = date,
            ["slotId"] = slot,
            ["recipeId"] = recipe,
            ["servings"] = servings,
            ["notes"] = notes,
            ["mealTime"] = mealTime,
        };

    private static Dictionary<string, object?> Update(Guid meal, Guid slot, Guid recipe, decimal servings, string date = "2026-11-05")
        => new() { ["mealId"] = meal, ["date"] = date, ["slotId"] = slot, ["recipeId"] = recipe, ["servings"] = servings };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));

    /// <summary>Imports a private recipe planned once for Lunch on 2026-11-05; returns the Lunch slot and the recipe.</summary>
    private async Task<(Guid Lunch, Guid Recipe)> SetUpAsync(string subject, string prefix = "Stew")
    {
        var rest = _mcp.Rest(subject);
        var recipe = $"{prefix} {_tag}";
        var import = new ImportDto(
            [new ImportProductDto($"{recipe} base", 120m, 4m, 20m, 2m, 3m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto($"{recipe} base", 250m, "g")])],
            [new ImportScheduleDto("2026-11-05", [new ImportMealDto("Lunch", recipe, 1m, null)])]);
        (await rest.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var schedule = await rest.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule", TestContext.Current.CancellationToken);
        var meal = (await RestDayAsync(subject, "2026-11-05")).Single();
        return (schedule!.Slots.Single(s => s.Name == "Lunch").Id, meal.RecipeId);
    }

    private async Task<IReadOnlyList<MealEntryDto>> RestDayAsync(string subject, string date)
        => (await _mcp.Rest(subject).GetFromJsonAsync<IReadOnlyList<MealEntryDto>>(
            $"/api/v1/meals?from={date}&to={date}", TestContext.Current.CancellationToken))!;

    /// <summary>Records holding lists compare by reference; compare the lists by content.</summary>
    private sealed class DiaryComparer : IEqualityComparer<DiaryMealDto>
    {
        public bool Equals(DiaryMealDto? x, DiaryMealDto? y)
            => x is not null && y is not null
                && x with { ActualProducts = [] } == y with { ActualProducts = [] }
                && x.ActualProducts.SequenceEqual(y.ActualProducts);

        public int GetHashCode(DiaryMealDto obj) => obj.Id.GetHashCode();
    }

    /// <summary>The plan fields of a meal entry; a failed call must leave them as they were.</summary>
    private sealed class MealComparer : IEqualityComparer<MealEntryDto>
    {
        public bool Equals(MealEntryDto? x, MealEntryDto? y)
            => x is not null && y is not null
                && (x.Id, x.Date, x.MealSlotId, x.RecipeId, x.Servings, x.Notes, x.MealTime, x.SequenceOrder, x.Status)
                == (y.Id, y.Date, y.MealSlotId, y.RecipeId, y.Servings, y.Notes, y.MealTime, y.SequenceOrder, y.Status);

        public int GetHashCode(MealEntryDto obj) => obj.Id.GetHashCode();
    }
}
