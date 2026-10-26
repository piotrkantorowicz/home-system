namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetShoppingList;
using DietPlanner.Application.Queries.GetWeeklySummary;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>get_shopping_list</c>, <c>check_shopping_item</c>, <c>clear_shopping_checks</c>
/// and <c>get_weekly_summary</c>: the same lists, ticks and totals as REST, scoped by household and date range.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpShoppingToolsTests(DatabaseFixture db) : IDisposable
{
    private const string From = "2026-11-02";
    private const string To = "2026-11-08";
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>The list sums every household member's planned meals and equals the REST list.</summary>
    [Fact]
    public async Task GetShoppingList_SumsHouseholdAndMatchesRest()
    {
        var (me, partner) = ($"sh-me-{_tag}", $"sh-partner-{_tag}");
        _mcp.Households.Join(me, partner);
        await PlanAsync(me, "Oats", "2026-11-03");
        await PlanAsync(partner, "Rice", "2026-11-04");
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var list = await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(client, "get_shopping_list", Range());
        var rest = await _mcp.Rest(me).GetFromJsonAsync<IReadOnlyList<ShoppingListItemDto>>($"/api/v1/meals/shopping-list?from={From}&to={To}", TestContext.Current.CancellationToken);

        list.ShouldBe(rest!);
        list.Select(i => i.ProductName).ShouldBe([$"Oats {_tag} base", $"Rice {_tag} base"], ignoreOrder: true);
        list.ShouldAllBe(i => i.TotalAmount == 250m && !i.IsChecked);
    }

    /// <summary>A tick is shared with the household, belongs to its range only, and clear removes it.</summary>
    [Fact]
    public async Task CheckAndClear_AreSharedAndScopedToTheRange()
    {
        var (me, partner) = ($"sh-check-{_tag}", $"sh-check-partner-{_tag}");
        _mcp.Households.Join(me, partner);
        await PlanAsync(me, "Oats", "2026-11-03");
        await using var mine = await _mcp.ConnectAsync(me, WriteScopes);
        await using var theirs = await _mcp.ConnectAsync(partner, WriteScopes);
        var item = (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(mine, "get_shopping_list", Range())).Single();

        var tick = await McpTestHost.CallAsync<ShoppingCheckDto>(mine, "check_shopping_item", Check(item, true));
        var again = await McpTestHost.CallAsync<ShoppingCheckDto>(mine, "check_shopping_item", Check(item, true));
        var partnerView = (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(theirs, "get_shopping_list", Range())).Single();
        var otherRange = (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(mine, "get_shopping_list", Range(From, "2026-11-05"))).Single();
        var cleared = await McpTestHost.CallAsync<ClearedShoppingChecksDto>(theirs, "clear_shopping_checks", Range());
        var after = (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(mine, "get_shopping_list", Range())).Single();

        tick.IsChecked.ShouldBeTrue();
        again.ShouldBe(tick);
        partnerView.IsChecked.ShouldBeTrue();
        otherRange.IsChecked.ShouldBeFalse();
        cleared.ShouldBe(new ClearedShoppingChecksDto(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8)));
        after.IsChecked.ShouldBeFalse();
    }

    /// <summary>A household guest may read the list but not tick it.</summary>
    [Fact]
    public async Task CheckShoppingItem_ByGuest_IsRefused()
    {
        var (owner, guest) = ($"sh-owner-{_tag}", $"sh-guest-{_tag}");
        _mcp.Households.Join(owner, guest);
        _mcp.Households.Contexts[guest] = _mcp.Households.Contexts[guest] with { Role = "Guest" };
        await PlanAsync(owner, "Oats", "2026-11-03");
        await using var client = await _mcp.ConnectAsync(guest, WriteScopes);
        var item = (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(client, "get_shopping_list", Range())).Single();

        var error = await McpTestHost.CallExpectingErrorAsync(client, "check_shopping_item", Check(item, true));

        error.ShouldContain("does not allow changing the shopping list");
        (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(client, "get_shopping_list", Range())).Single().IsChecked.ShouldBeFalse();
    }

    /// <summary>Someone outside the household sees none of the caller's items, and their ticks do not reach the caller's list.</summary>
    [Fact]
    public async Task ShoppingTools_ForStranger_SeeNothingAndChangeNothing()
    {
        var (me, stranger) = ($"sh-alone-{_tag}", $"sh-stranger-{_tag}");
        await PlanAsync(me, "Oats", "2026-11-03");
        await using var mine = await _mcp.ConnectAsync(me, WriteScopes);
        await using var theirs = await _mcp.ConnectAsync(stranger, WriteScopes);
        var item = (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(mine, "get_shopping_list", Range())).Single();

        var view = await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(theirs, "get_shopping_list", Range());
        await McpTestHost.CallAsync<ShoppingCheckDto>(theirs, "check_shopping_item", Check(item, true));

        view.ShouldBeEmpty();
        (await McpTestHost.CallAsync<IReadOnlyList<ShoppingListItemDto>>(mine, "get_shopping_list", Range())).Single().IsChecked.ShouldBeFalse();
    }

    /// <summary>The weekly summary equals REST and counts only the caller's own diary.</summary>
    [Fact]
    public async Task GetWeeklySummary_MatchesRestForTheCallerOnly()
    {
        var (me, partner) = ($"sh-summary-{_tag}", $"sh-summary-partner-{_tag}");
        _mcp.Households.Join(me, partner);
        await PlanAsync(me, "Oats", "2026-11-03");
        await PlanAsync(partner, "Rice", "2026-11-04");
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        await McpTestHost.CallAsync<CompletedDayDto>(client, "complete_day", new() { ["date"] = "2026-11-03" });

        var summary = await McpTestHost.CallAsync<WeeklySummaryDto>(client, "get_weekly_summary", new() { ["weekStart"] = From, ["weekEnd"] = To });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<WeeklySummaryDto>($"/api/v1/weekly-summary?weekStart={From}&weekEnd={To}", TestContext.Current.CancellationToken);

        summary.ShouldBe(rest);
        summary.MealsPlanned.ShouldBe(1);
        summary.MealsCompleted.ShouldBe(1);
        summary.TotalKcal.ShouldBe(300);
    }

    /// <summary>Reversed or over-long ranges are refused before anything runs.</summary>
    [Theory]
    [InlineData("get_shopping_list", "from", "to")]
    [InlineData("clear_shopping_checks", "from", "to")]
    [InlineData("get_weekly_summary", "weekStart", "weekEnd")]
    public async Task RangeTools_RejectReversedAndLongRanges(string tool, string start, string end)
    {
        await using var client = await _mcp.ConnectAsync($"sh-range-{_tag}", WriteScopes);

        var reversed = await McpTestHost.CallExpectingErrorAsync(client, tool, new() { [start] = To, [end] = From });
        var tooLong = await McpTestHost.CallExpectingErrorAsync(client, tool, new() { [start] = "2026-10-01", [end] = "2026-11-30" });

        reversed.ShouldContain("must not be before");
        tooLong.ShouldContain("at most 31");
    }

    /// <summary>A read-only token reads the list and summary but neither lists nor runs the tick tools.</summary>
    [Theory]
    [InlineData("check_shopping_item")]
    [InlineData("clear_shopping_checks")]
    public async Task ShoppingWrites_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        await using var client = await _mcp.ConnectAsync($"sh-ro-{_tag}", DietPlannerMcp.ReadScope);
        var args = Range();
        args["productId"] = Guid.CreateVersion7();
        args["unit"] = "g";
        args["isChecked"] = true;

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, args, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        tools.ShouldContain(t => t.Name == "get_shopping_list");
        tools.ShouldContain(t => t.Name == "get_weekly_summary");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <summary>The assistant can run the shopping and summary tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task ShoppingTools_InProcess()
    {
        var me = $"sh-inproc-{_tag}";
        await PlanAsync(me, "Oats", "2026-11-03");
        var item = (await _mcp.Rest(me).GetFromJsonAsync<IReadOnlyList<ShoppingListItemDto>>($"/api/v1/meals/shopping-list?from={From}&to={To}", TestContext.Current.CancellationToken))!.Single();
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var results = new[]
        {
            await invoker.InvokeAsync(user, "get_shopping_list", Json(Range()), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "check_shopping_item", Json(Check(item, true)), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "clear_shopping_checks", Json(Range()), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "get_weekly_summary", Json(new() { ["weekStart"] = From, ["weekEnd"] = To }), TestContext.Current.CancellationToken),
        };

        results.ShouldAllBe(r => r.IsError != true);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Range(string from = From, string to = To) => new() { ["from"] = from, ["to"] = to };

    private static Dictionary<string, object?> Check(ShoppingListItemDto item, bool isChecked)
        => new() { ["from"] = From, ["to"] = To, ["productId"] = item.ProductId, ["unit"] = item.Unit, ["isChecked"] = isChecked };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));

    /// <summary>Imports a one-ingredient recipe (250 g of a 120 kcal/100 g product) planned once for Lunch on <paramref name="date"/>.</summary>
    private async Task PlanAsync(string subject, string prefix, string date)
    {
        var recipe = $"{prefix} {_tag}";
        var import = new ImportDto(
            [new ImportProductDto($"{recipe} base", 120m, 4m, 20m, 2m, 3m, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto($"{recipe} base", 250m, "g")])],
            [new ImportScheduleDto(date, [new ImportMealDto("Lunch", recipe, 1m, null)])]);
        (await _mcp.Rest(subject).PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
    }
}
