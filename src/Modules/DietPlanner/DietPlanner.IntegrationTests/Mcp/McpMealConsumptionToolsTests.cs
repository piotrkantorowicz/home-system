namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>complete_meal</c>, <c>override_meal</c>, <c>reset_meal</c> and <c>complete_day</c>:
/// the same status transitions and nutrition as REST, through real dispatchers and PostgreSQL.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpMealConsumptionToolsTests(DatabaseFixture db) : IDisposable
{
    private const string Day = "2026-11-05";
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>Complete marks Done and repeats harmlessly; a Modified meal is refused until reset.</summary>
    [Fact]
    public async Task CompleteMeal_FollowsRestTransitions()
    {
        var me = $"mc-complete-{_tag}";
        var (meals, product) = await SetUpAsync(me);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var done = await McpTestHost.CallAsync<MealStatusDto>(client, "complete_meal", Meal(meals[0]));
        var again = await McpTestHost.CallAsync<MealStatusDto>(client, "complete_meal", Meal(meals[0]));
        await McpTestHost.CallAsync<MealStatusDto>(client, "override_meal", Override(meals[1], product, 40m));
        var modified = await McpTestHost.CallExpectingErrorAsync(client, "complete_meal", Meal(meals[1]));
        var rest = await RestDayAsync(me);

        done.Status.ShouldBe("Done");
        again.Status.ShouldBe("Done");
        modified.ShouldContain("Reset the override first");
        rest.Single(m => m.Id == meals[0]).Status.ShouldBe("Done");
        rest.Single(m => m.Id == meals[1]).Status.ShouldBe("Modified");
    }

    /// <summary>An override counts what was eaten, exactly as the REST override does, and replaces an earlier one.</summary>
    [Fact]
    public async Task OverrideMeal_MatchesRestAndReplacesEarlierOverride()
    {
        var (me, viaRest) = ($"mc-override-{_tag}", $"mc-override-rest-{_tag}");
        var (meals, product) = await SetUpAsync(me);
        var (restMeals, restProduct) = await SetUpAsync(viaRest);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        await McpTestHost.CallAsync<MealStatusDto>(client, "override_meal", Override(meals[0], product, 999m));
        var status = await McpTestHost.CallAsync<MealStatusDto>(client, "override_meal", Override(meals[0], product, 80m));
        (await _mcp.Rest(viaRest).PatchAsJsonAsync($"/api/v1/meals/{restMeals[0]}/override",
            new OverrideMealEntryRequest(null, [new ActualProductRequest(restProduct, 80m, "g")]), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var mine = (await RestDayAsync(me)).Single(m => m.Id == meals[0]);
        var theirs = (await RestDayAsync(viaRest)).Single(m => m.Id == restMeals[0]);
        var neither = await McpTestHost.CallExpectingErrorAsync(client, "override_meal", Meal(meals[1]));

        status.Status.ShouldBe("Modified");
        mine.ActualProducts.ShouldHaveSingleItem().Amount.ShouldBe(80m);
        (mine.Calories, mine.Protein, mine.Carbs, mine.Fat, mine.Fiber).ShouldBe((theirs.Calories, theirs.Protein, theirs.Carbs, theirs.Fat, theirs.Fiber));
        neither.ShouldContain("either a replacement recipe or at least one product");
    }

    /// <summary>Reset discards completion and override data; repeating it changes nothing.</summary>
    [Fact]
    public async Task ResetMeal_ClearsOverrideAndRepeatsHarmlessly()
    {
        var me = $"mc-reset-{_tag}";
        var (meals, product) = await SetUpAsync(me);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        await McpTestHost.CallAsync<MealStatusDto>(client, "override_meal", Override(meals[0], product, 40m));

        var reset = await McpTestHost.CallAsync<MealStatusDto>(client, "reset_meal", Meal(meals[0]));
        var again = await McpTestHost.CallAsync<MealStatusDto>(client, "reset_meal", Meal(meals[0]));
        var meal = (await RestDayAsync(me)).Single(m => m.Id == meals[0]);

        reset.Status.ShouldBe("Planned");
        again.Status.ShouldBe("Planned");
        meal.Status.ShouldBe("Planned");
        meal.ActualProducts.ShouldBeEmpty();
    }

    /// <summary>complete_day completes only Planned meals, leaves an override alone, and returns 0 on a repeat, like REST bulk-complete.</summary>
    [Fact]
    public async Task CompleteDay_MatchesRestBulkComplete()
    {
        var (me, viaRest) = ($"mc-day-{_tag}", $"mc-day-rest-{_tag}");
        var (meals, product) = await SetUpAsync(me);
        var (restMeals, restProduct) = await SetUpAsync(viaRest);
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        await McpTestHost.CallAsync<MealStatusDto>(client, "override_meal", Override(meals[2], product, 40m));
        (await _mcp.Rest(viaRest).PatchAsJsonAsync($"/api/v1/meals/{restMeals[2]}/override",
            new OverrideMealEntryRequest(null, [new ActualProductRequest(restProduct, 40m, "g")]), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var first = await McpTestHost.CallAsync<CompletedDayDto>(client, "complete_day", new() { ["date"] = Day });
        var repeat = await McpTestHost.CallAsync<CompletedDayDto>(client, "complete_day", new() { ["date"] = Day });
        var rest = await (await _mcp.Rest(viaRest).PostAsJsonAsync("/api/v1/meals/bulk-complete",
            new BulkCompleteMealsRequest(new DateOnly(2026, 11, 5)), TestContext.Current.CancellationToken)).Content
            .ReadFromJsonAsync<BulkCompleteMealsResponse>(TestContext.Current.CancellationToken);

        first.Completed.ShouldBe(rest!.Completed);
        first.Completed.ShouldBe(2);
        repeat.Completed.ShouldBe(0);
        (await RestDayAsync(me)).Select(m => m.Status).ShouldBe((await RestDayAsync(viaRest)).Select(m => m.Status));
    }

    /// <summary>Someone outside the household cannot change the caller's meals, and complete_day only touches their own diary.</summary>
    [Fact]
    public async Task ConsumptionTools_ForStranger_FailLikeMissingAndChangeNothing()
    {
        var (me, stranger) = ($"mc-me-{_tag}", $"mc-stranger-{_tag}");
        var (meals, product) = await SetUpAsync(me);
        await using var client = await _mcp.ConnectAsync(stranger, WriteScopes);

        var complete = await McpTestHost.CallExpectingErrorAsync(client, "complete_meal", Meal(meals[0]));
        var over = await McpTestHost.CallExpectingErrorAsync(client, "override_meal", Override(meals[0], product, 40m));
        var reset = await McpTestHost.CallExpectingErrorAsync(client, "reset_meal", Meal(meals[0]));
        var day = await McpTestHost.CallAsync<CompletedDayDto>(client, "complete_day", new() { ["date"] = Day });

        complete.ShouldContain("not found", Case.Insensitive);
        over.ShouldContain("not found", Case.Insensitive);
        reset.ShouldContain("not found", Case.Insensitive);
        day.Completed.ShouldBe(0);
        (await RestDayAsync(me)).ShouldAllBe(m => m.Status == "Planned");
    }

    /// <summary>A read-only token neither lists nor runs the consumption tools.</summary>
    [Theory]
    [InlineData("complete_meal")]
    [InlineData("override_meal")]
    [InlineData("reset_meal")]
    [InlineData("complete_day")]
    public async Task ConsumptionTools_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"mc-ro-{_tag}";
        var (meals, product) = await SetUpAsync(me);
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var args = Override(meals[0], product, 40m);
        args["date"] = Day;

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, args, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await RestDayAsync(me)).ShouldAllBe(m => m.Status == "Planned");
    }

    /// <summary>The assistant can run the consumption tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task ConsumptionTools_InProcess()
    {
        var me = $"mc-inproc-{_tag}";
        var (meals, product) = await SetUpAsync(me);
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var results = new[]
        {
            await invoker.InvokeAsync(user, "override_meal", Json(Override(meals[0], product, 40m)), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "reset_meal", Json(Meal(meals[0])), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "complete_meal", Json(Meal(meals[1])), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "complete_day", Json(new() { ["date"] = Day }), TestContext.Current.CancellationToken),
        };

        results.ShouldAllBe(r => r.IsError != true);
        (await RestDayAsync(me)).ShouldAllBe(m => m.Status == "Done");
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Meal(Guid id) => new() { ["mealId"] = id };

    private static Dictionary<string, object?> Override(Guid meal, Guid product, decimal grams)
        => new() { ["mealId"] = meal, ["actualProducts"] = new[] { new { productId = product, amount = grams, unit = "g" } } };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));

    /// <summary>Imports a recipe planned for three slots on <see cref="Day"/>; returns the meals in slot order and the recipe's product.</summary>
    private async Task<(IReadOnlyList<Guid> Meals, Guid Product)> SetUpAsync(string subject)
    {
        var rest = _mcp.Rest(subject);
        var (recipe, product) = ($"Stew {_tag}", $"Stew {_tag} base");
        var import = new ImportDto(
            [new ImportProductDto(product, 120m, 4m, 20m, 2m, 3m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto(product, 250m, "g")])],
            [new ImportScheduleDto(Day, [
                new ImportMealDto("Breakfast", recipe, 1m, null),
                new ImportMealDto("Lunch", recipe, 1m, null),
                new ImportMealDto("Dinner", recipe, 1m, null)])]);
        (await rest.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using var client = await _mcp.ConnectAsync(subject, DietPlannerMcp.ReadScope);
        var productId = (await McpTestHost.CallAsync<CatalogPageDto<CatalogProductDto>>(client, "search_products", new() { ["query"] = product })).Items.Single().Id;
        return ([.. (await RestDayAsync(subject)).OrderBy(m => m.MealSlotSortOrder).Select(m => m.Id)], productId);
    }

    private async Task<IReadOnlyList<MealEntryDto>> RestDayAsync(string subject)
        => [.. (await _mcp.Rest(subject).GetFromJsonAsync<IReadOnlyList<MealEntryDto>>(
            $"/api/v1/meals?from={Day}&to={Day}", TestContext.Current.CancellationToken))!.OrderBy(m => m.MealSlotSortOrder)];
}
