namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Application.Queries.GetHydrationConfig;
using DietPlanner.Application.Queries.GetWaterIntake;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>get_hydration</c>, <c>update_hydration_config</c>, <c>log_water_intake</c> and
/// <c>delete_water_intake</c>: the same settings and drink log as REST, kept apart, scoped to the caller.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpHydrationToolsTests(DatabaseFixture db) : IDisposable
{
    private const string Day = "2026-02-10";
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>Settings are created then replaced, match REST, and changing them leaves logged drinks alone.</summary>
    [Fact]
    public async Task UpdateHydrationConfig_CreatesThenReplaces_WithoutTouchingDrinks()
    {
        var me = $"hy-config-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var empty = await McpTestHost.CallAsync<HydrationDto>(client, "get_hydration", new() { ["date"] = Day });
        await McpTestHost.CallAsync<HydrationConfigDto>(client, "update_hydration_config", Config(2500, 250, true));
        await McpTestHost.CallAsync<LoggedWaterIntakeDto>(client, "log_water_intake", Drink(Key(), 250));
        var replaced = await McpTestHost.CallAsync<HydrationConfigDto>(client, "update_hydration_config", Config(3000, 330, false));
        var rest = await _mcp.Rest(me).GetFromJsonAsync<HydrationConfigDto>("/api/v1/hydration/config", TestContext.Current.CancellationToken);
        var after = await McpTestHost.CallAsync<HydrationDto>(client, "get_hydration", new() { ["date"] = Day });

        empty.Config.ShouldBeNull();
        empty.Intake.TotalMl.ShouldBe(0);
        replaced.ShouldBe(rest);
        replaced.ShouldSatisfyAllConditions(
            c => c.DailyWaterTargetMl.ShouldBe(3000),
            c => c.GlassSizeMl.ShouldBe(330),
            c => c.TrackWaterIntake.ShouldBeFalse());
        after.Intake.TotalMl.ShouldBe(250);
    }

    /// <summary>Out-of-range settings and amounts are field errors and save nothing.</summary>
    [Fact]
    public async Task HydrationTools_RejectOutOfRangeValues()
    {
        var me = $"hy-invalid-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var target = await McpTestHost.CallExpectingErrorAsync(client, "update_hydration_config", Config(50, 250, true));
        var glass = await McpTestHost.CallExpectingErrorAsync(client, "update_hydration_config", Config(2000, 10, true));
        var amount = await McpTestHost.CallExpectingErrorAsync(client, "log_water_intake", Drink(Key(), 6000));
        var hydration = await McpTestHost.CallAsync<HydrationDto>(client, "get_hydration", new() { ["date"] = Day });

        target.ShouldContain("DailyWaterTargetMl");
        glass.ShouldContain("GlassSizeMl");
        amount.ShouldContain("AmountMl");
        hydration.Config.ShouldBeNull();
        hydration.Intake.Entries.ShouldBeEmpty();
    }

    /// <summary>A retried drink with the same key is counted once; a new key adds another; the day equals REST.</summary>
    [Fact]
    public async Task LogWaterIntake_ReplaysByKeyAndMatchesRest()
    {
        var me = $"hy-log-{_tag}";
        var key = Key();
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var first = await McpTestHost.CallAsync<LoggedWaterIntakeDto>(client, "log_water_intake", Drink(key, 300, "morning"));
        var retry = await McpTestHost.CallAsync<LoggedWaterIntakeDto>(client, "log_water_intake", Drink(key, 300, "morning"));
        var conflict = await McpTestHost.CallExpectingErrorAsync(client, "log_water_intake", Drink(key, 500, "morning"));
        await McpTestHost.CallAsync<LoggedWaterIntakeDto>(client, "log_water_intake", Drink(Key(), 200));
        var day = await McpTestHost.CallAsync<HydrationDto>(client, "get_hydration", new() { ["date"] = Day });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<WaterIntakeListDto>($"/api/v1/hydration/intake?date={Day}", TestContext.Current.CancellationToken);

        retry.IntakeId.ShouldBe(first.IntakeId);
        conflict.ShouldContain("already used for a different");
        day.Intake.TotalMl.ShouldBe(500);
        day.Intake.Entries.Count.ShouldBe(2);
        day.Intake.Entries.Select(e => e.Id).ShouldBe(rest!.Entries.Select(e => e.Id));
        day.Intake.TotalMl.ShouldBe(rest.TotalMl);
    }

    /// <summary>Delete lowers the day's total; a repeat reports not found.</summary>
    [Fact]
    public async Task DeleteWaterIntake_LowersTotal()
    {
        var me = $"hy-delete-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var drink = await McpTestHost.CallAsync<LoggedWaterIntakeDto>(client, "log_water_intake", Drink(Key(), 400));
        await McpTestHost.CallAsync<LoggedWaterIntakeDto>(client, "log_water_intake", Drink(Key(), 100));

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(client, "delete_water_intake", new() { ["intakeId"] = drink.IntakeId });
        var again = await McpTestHost.CallExpectingErrorAsync(client, "delete_water_intake", new() { ["intakeId"] = drink.IntakeId });
        var day = await McpTestHost.CallAsync<HydrationDto>(client, "get_hydration", new() { ["date"] = Day });

        deleted.Id.ShouldBe(drink.IntakeId);
        again.ShouldContain("not found", Case.Insensitive);
        day.Intake.TotalMl.ShouldBe(100);
    }

    /// <summary>Another person sees none of the caller's drinks or settings, and deleting the caller's drink reads as missing.</summary>
    [Fact]
    public async Task HydrationTools_ForStranger_SeeNothingAndChangeNothing()
    {
        var (me, stranger) = ($"hy-me-{_tag}", $"hy-stranger-{_tag}");
        await using var mine = await _mcp.ConnectAsync(me, WriteScopes);
        await using var theirs = await _mcp.ConnectAsync(stranger, WriteScopes);
        await McpTestHost.CallAsync<HydrationConfigDto>(mine, "update_hydration_config", Config(2500, 250, true));
        var drink = await McpTestHost.CallAsync<LoggedWaterIntakeDto>(mine, "log_water_intake", Drink(Key(), 250));

        var view = await McpTestHost.CallAsync<HydrationDto>(theirs, "get_hydration", new() { ["date"] = Day });
        var delete = await McpTestHost.CallExpectingErrorAsync(theirs, "delete_water_intake", new() { ["intakeId"] = drink.IntakeId });

        view.Config.ShouldBeNull();
        view.Intake.Entries.ShouldBeEmpty();
        delete.ShouldContain("not found", Case.Insensitive);
        (await McpTestHost.CallAsync<HydrationDto>(mine, "get_hydration", new() { ["date"] = Day })).Intake.TotalMl.ShouldBe(250);
    }

    /// <summary>A read-only token can read hydration but neither lists nor runs the writes.</summary>
    [Theory]
    [InlineData("update_hydration_config")]
    [InlineData("log_water_intake")]
    [InlineData("delete_water_intake")]
    public async Task HydrationWrites_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"hy-ro-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var args = Drink(Key(), 250);
        foreach (var (k, v) in Config(2500, 250, true))
            args[k] = v;
        args["intakeId"] = Guid.CreateVersion7();

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, args, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        tools.ShouldContain(t => t.Name == "get_hydration");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await McpTestHost.CallAsync<HydrationDto>(client, "get_hydration", new() { ["date"] = Day })).Config.ShouldBeNull();
    }

    /// <summary>The assistant can run the hydration tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task HydrationTools_InProcess()
    {
        var me = $"hy-inproc-{_tag}";
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var config = await invoker.InvokeAsync(user, "update_hydration_config", Json(Config(2000, 250, true)), TestContext.Current.CancellationToken);
        var log = await invoker.InvokeAsync(user, "log_water_intake", Json(Drink(Key(), 250)), TestContext.Current.CancellationToken);
        var entry = (await _mcp.Rest(me).GetFromJsonAsync<WaterIntakeListDto>($"/api/v1/hydration/intake?date={Day}", TestContext.Current.CancellationToken))!.Entries.Single();
        var read = await invoker.InvokeAsync(user, "get_hydration", Json(new() { ["date"] = Day }), TestContext.Current.CancellationToken);
        var delete = await invoker.InvokeAsync(user, "delete_water_intake", Json(new() { ["intakeId"] = entry.Id }), TestContext.Current.CancellationToken);

        new[] { config, log, read, delete }.ShouldAllBe(r => r.IsError != true);
        (await _mcp.Rest(me).GetFromJsonAsync<WaterIntakeListDto>($"/api/v1/hydration/intake?date={Day}", TestContext.Current.CancellationToken))!.Entries.ShouldBeEmpty();
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static string Key() => Guid.CreateVersion7().ToString();

    private static Dictionary<string, object?> Config(int target, int glass, bool track)
        => new() { ["dailyWaterTargetMl"] = target, ["glassSizeMl"] = glass, ["trackWaterIntake"] = track };

    private static Dictionary<string, object?> Drink(string key, int ml, string? note = null)
        => new() { ["operationKey"] = key, ["date"] = Day, ["amountMl"] = ml, ["note"] = note };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));
}
