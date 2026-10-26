namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Application.Commands.LogWeightEntry;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.Application.Queries.GetWeightEntries;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>log_weight</c>, <c>get_weight_entries</c> and <c>delete_weight_entry</c>:
/// one weigh-in per day, the same log and current weight as REST, scoped to the caller.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpWeightToolsTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>Logging a day twice corrects it; the log and the profile's current weight match REST.</summary>
    [Fact]
    public async Task LogWeight_SameDayTwice_CorrectsAndMatchesRest()
    {
        var me = $"wt-log-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        await McpTestHost.CallAsync<UserProfileDto>(client, "set_profile", new() { ["heightCm"] = 175m });

        var first = await McpTestHost.CallAsync<LogWeightEntryResult>(client, "log_weight", Weigh("2025-03-01", 80.4m));
        var retry = await McpTestHost.CallAsync<LogWeightEntryResult>(client, "log_weight", Weigh("2025-03-01", 80.2m));
        await McpTestHost.CallAsync<LogWeightEntryResult>(client, "log_weight", Weigh("2025-03-08", 79.6m));
        var entries = await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(client, "get_weight_entries", []);
        var rest = await _mcp.Rest(me).GetFromJsonAsync<IReadOnlyList<WeightEntryDto>>("/api/v1/weight-entries", TestContext.Current.CancellationToken);
        var profile = await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", []);

        first.Created.ShouldBeTrue();
        retry.ShouldBe(first with { Created = false });
        entries.ShouldBe(rest!);
        entries.Select(e => (e.Date, e.WeightKg)).ShouldBe([(new DateOnly(2025, 3, 1), 80.2m), (new DateOnly(2025, 3, 8), 79.6m)]);
        profile.Profile!.CurrentWeightKg.ShouldBe(79.6m);
    }

    /// <summary>Without a profile there is no current weight to set, so logging is refused, as in REST.</summary>
    [Fact]
    public async Task LogWeight_WithoutProfile_IsRefused()
    {
        await using var client = await _mcp.ConnectAsync($"wt-noprofile-{_tag}", WriteScopes);

        var error = await McpTestHost.CallExpectingErrorAsync(client, "log_weight", Weigh("2025-03-01", 80m));

        error.ShouldContain("UserProfile");
        (await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(client, "get_weight_entries", [])).ShouldBeEmpty();
    }

    /// <summary>Out-of-range weights, future days and a reversed range are refused.</summary>
    [Fact]
    public async Task WeightTools_RejectInvalidInput()
    {
        var me = $"wt-invalid-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var zero = await McpTestHost.CallExpectingErrorAsync(client, "log_weight", Weigh("2025-03-01", 0m));
        var huge = await McpTestHost.CallExpectingErrorAsync(client, "log_weight", Weigh("2025-03-01", 1000m));
        var future = await McpTestHost.CallExpectingErrorAsync(client, "log_weight", Weigh("2099-01-01", 80m));
        var range = await McpTestHost.CallExpectingErrorAsync(client, "get_weight_entries", new() { ["from"] = "2025-03-08", ["to"] = "2025-03-01" });

        zero.ShouldContain("WeightKg");
        huge.ShouldContain("WeightKg");
        future.ShouldContain("future");
        range.ShouldContain("on or before");
        (await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(client, "get_weight_entries", [])).ShouldBeEmpty();
    }

    /// <summary>The range filter is inclusive, as in REST.</summary>
    [Fact]
    public async Task GetWeightEntries_FiltersInclusiveRangeLikeRest()
    {
        var me = $"wt-range-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        await McpTestHost.CallAsync<UserProfileDto>(client, "set_profile", []);
        foreach (var (day, kg) in new[] { ("2025-04-01", 81m), ("2025-04-05", 80.5m), ("2025-04-09", 80m) })
            await McpTestHost.CallAsync<LogWeightEntryResult>(client, "log_weight", Weigh(day, kg));

        var mcp = await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(client, "get_weight_entries", new() { ["from"] = "2025-04-05", ["to"] = "2025-04-09" });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<IReadOnlyList<WeightEntryDto>>("/api/v1/weight-entries?from=2025-04-05&to=2025-04-09", TestContext.Current.CancellationToken);

        mcp.ShouldBe(rest!);
        mcp.Count.ShouldBe(2);
    }

    /// <summary>Delete falls back to the latest remaining weigh-in for the current weight; a repeat reports not found.</summary>
    [Fact]
    public async Task DeleteWeightEntry_RecomputesCurrentWeight()
    {
        var me = $"wt-delete-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        await McpTestHost.CallAsync<UserProfileDto>(client, "set_profile", new() { ["heightCm"] = 175m });
        await McpTestHost.CallAsync<LogWeightEntryResult>(client, "log_weight", Weigh("2025-05-01", 82m));
        var latest = await McpTestHost.CallAsync<LogWeightEntryResult>(client, "log_weight", Weigh("2025-05-08", 81m));

        var deleted = await McpTestHost.CallAsync<DeletedItemDto>(client, "delete_weight_entry", new() { ["entryId"] = latest.Id });
        var again = await McpTestHost.CallExpectingErrorAsync(client, "delete_weight_entry", new() { ["entryId"] = latest.Id });
        var context = await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", []);

        deleted.Id.ShouldBe(latest.Id);
        again.ShouldContain("not found", Case.Insensitive);
        context.Profile!.CurrentWeightKg.ShouldBe(82m);
        (await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(client, "get_weight_entries", [])).ShouldHaveSingleItem();
    }

    /// <summary>Another person cannot see or delete the caller's weigh-ins.</summary>
    [Fact]
    public async Task WeightTools_ForStranger_SeeNothingAndChangeNothing()
    {
        var (me, stranger) = ($"wt-me-{_tag}", $"wt-stranger-{_tag}");
        await using var mine = await _mcp.ConnectAsync(me, WriteScopes);
        await using var theirs = await _mcp.ConnectAsync(stranger, WriteScopes);
        await McpTestHost.CallAsync<UserProfileDto>(mine, "set_profile", []);
        var entry = await McpTestHost.CallAsync<LogWeightEntryResult>(mine, "log_weight", Weigh("2025-06-01", 70m));

        var list = await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(theirs, "get_weight_entries", []);
        var delete = await McpTestHost.CallExpectingErrorAsync(theirs, "delete_weight_entry", new() { ["entryId"] = entry.Id });

        list.ShouldBeEmpty();
        delete.ShouldContain("not found", Case.Insensitive);
        (await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(mine, "get_weight_entries", [])).ShouldHaveSingleItem();
    }

    /// <summary>A read-only token can list weigh-ins but neither lists nor runs the writes.</summary>
    [Theory]
    [InlineData("log_weight")]
    [InlineData("delete_weight_entry")]
    public async Task WeightWrites_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"wt-ro-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var args = Weigh("2025-06-01", 70m);
        args["entryId"] = Guid.CreateVersion7();

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, args, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        tools.ShouldContain(t => t.Name == "get_weight_entries");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await McpTestHost.CallAsync<IReadOnlyList<WeightEntryDto>>(client, "get_weight_entries", [])).ShouldBeEmpty();
    }

    /// <summary>The assistant can run the weight tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task WeightTools_InProcess()
    {
        var me = $"wt-inproc-{_tag}";
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var profile = await invoker.InvokeAsync(user, "set_profile", Json([]), TestContext.Current.CancellationToken);
        var log = await invoker.InvokeAsync(user, "log_weight", Json(Weigh("2025-07-01", 75m)), TestContext.Current.CancellationToken);
        var entry = (await _mcp.Rest(me).GetFromJsonAsync<IReadOnlyList<WeightEntryDto>>("/api/v1/weight-entries", TestContext.Current.CancellationToken))!.Single();
        var list = await invoker.InvokeAsync(user, "get_weight_entries", Json([]), TestContext.Current.CancellationToken);
        var delete = await invoker.InvokeAsync(user, "delete_weight_entry", Json(new() { ["entryId"] = entry.Id }), TestContext.Current.CancellationToken);

        new[] { profile, log, list, delete }.ShouldAllBe(r => r.IsError != true);
        (await _mcp.Rest(me).GetFromJsonAsync<IReadOnlyList<WeightEntryDto>>("/api/v1/weight-entries", TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> Weigh(string date, decimal kg) => new() { ["date"] = date, ["weightKg"] = kg };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));
}
