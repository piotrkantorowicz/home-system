namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Application.Queries.GetGoal;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.Application.Queries.GetWeightPrediction;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for <c>set_profile</c>, <c>set_goals</c> and <c>get_weight_prediction</c>: the same
/// records and calculations as REST, scoped to the caller, through real dispatchers and PostgreSQL.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpDietSetupToolsTests(DatabaseFixture db) : IDisposable
{
    private static readonly string WriteScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope}";

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>The first call creates the profile, the next replaces it (left-out values become unknown); REST reads the same.</summary>
    [Fact]
    public async Task SetProfile_CreatesThenReplaces_MatchingRest()
    {
        var me = $"ds-profile-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var created = await McpTestHost.CallAsync<UserProfileDto>(client, "set_profile", FullProfile());
        var replaced = await McpTestHost.CallAsync<UserProfileDto>(client, "set_profile", new()
        {
            ["heightCm"] = 181m,
            ["currentWeightKg"] = 82.5m,
        });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<UserProfileDto>("/api/v1/profile", TestContext.Current.CancellationToken);

        replaced.Id.ShouldBe(created.Id);
        replaced.ShouldBe(rest);
        replaced.ShouldSatisfyAllConditions(
            p => p.HeightCm.ShouldBe(181m),
            p => p.CurrentWeightKg.ShouldBe(82.5m),
            p => p.DateOfBirth.ShouldBeNull(),
            p => p.Gender.ShouldBeNull());
    }

    /// <summary>Invalid values are field errors and save nothing.</summary>
    [Fact]
    public async Task SetProfile_WithInvalidValues_FailsWithoutSaving()
    {
        var me = $"ds-invalid-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var gender = await McpTestHost.CallExpectingErrorAsync(client, "set_profile", new() { ["gender"] = "Robot" });
        var height = await McpTestHost.CallExpectingErrorAsync(client, "set_profile", new() { ["heightCm"] = 0m });
        var context = await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", []);

        gender.ShouldContain("Gender");
        height.ShouldContain("HeightCm");
        context.Profile.ShouldBeNull();
    }

    /// <summary>The first call creates the goal, the next replaces it; REST reads the same.</summary>
    [Fact]
    public async Task SetGoals_CreatesThenReplaces_MatchingRest()
    {
        var me = $"ds-goal-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);

        var created = await McpTestHost.CallAsync<GoalDto>(client, "set_goals", new() { ["dailyCalorieTarget"] = 2200, ["proteinGrams"] = 140m });
        var replaced = await McpTestHost.CallAsync<GoalDto>(client, "set_goals", new() { ["dailyCalorieTarget"] = 2000, ["fiberGrams"] = 30m });
        var negative = await McpTestHost.CallExpectingErrorAsync(client, "set_goals", new() { ["fatGrams"] = -1m });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<GoalDto>("/api/v1/goals", TestContext.Current.CancellationToken);

        replaced.Id.ShouldBe(created.Id);
        replaced.ShouldBe(rest);
        replaced.ShouldSatisfyAllConditions(
            g => g.DailyCalorieTarget.ShouldBe(2000),
            g => g.ProteinGrams.ShouldBeNull(),
            g => g.FiberGrams.ShouldBe(30m));
        negative.ShouldContain("FatGrams");
    }

    /// <summary>The prediction equals the REST calculation; without a complete profile it says what is missing.</summary>
    [Fact]
    public async Task GetWeightPrediction_MatchesRest_AndNeedsCompleteProfile()
    {
        var me = $"ds-predict-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, WriteScopes);
        var incomplete = await McpTestHost.CallExpectingErrorAsync(client, "get_weight_prediction", new() { ["dailyCalorieTarget"] = 1800m });
        await McpTestHost.CallAsync<UserProfileDto>(client, "set_profile", FullProfile());

        var prediction = await McpTestHost.CallAsync<WeightPredictionDto>(client, "get_weight_prediction", new() { ["dailyCalorieTarget"] = 1800m });
        var rest = await _mcp.Rest(me).GetFromJsonAsync<WeightPredictionDto>("/api/v1/profile/prediction?dailyCalorieTarget=1800", TestContext.Current.CancellationToken);

        incomplete.ShouldContain("set_profile");
        prediction.ShouldBe(rest);
        prediction.DailyDeficit.ShouldBePositive();
    }

    /// <summary>Each caller only ever sees and changes their own profile and goal.</summary>
    [Fact]
    public async Task SetupTools_AreScopedToTheCaller()
    {
        var (me, other) = ($"ds-me-{_tag}", $"ds-other-{_tag}");
        await using var mine = await _mcp.ConnectAsync(me, WriteScopes);
        await using var theirs = await _mcp.ConnectAsync(other, WriteScopes);
        await McpTestHost.CallAsync<UserProfileDto>(mine, "set_profile", FullProfile());
        await McpTestHost.CallAsync<GoalDto>(mine, "set_goals", new() { ["dailyCalorieTarget"] = 2200 });

        var before = await McpTestHost.CallAsync<DietContextDto>(theirs, "get_diet_context", []);
        await McpTestHost.CallAsync<UserProfileDto>(theirs, "set_profile", new() { ["heightCm"] = 160m });
        await McpTestHost.CallAsync<GoalDto>(theirs, "set_goals", new() { ["dailyCalorieTarget"] = 1500 });
        var after = await McpTestHost.CallAsync<DietContextDto>(mine, "get_diet_context", []);

        before.Profile.ShouldBeNull();
        before.Goal.ShouldBeNull();
        after.Profile!.HeightCm.ShouldBe(178m);
        after.Goal!.DailyCalorieTarget.ShouldBe(2200);
    }

    /// <summary>A read-only token neither lists nor runs the setup writes, but may run the prediction.</summary>
    [Theory]
    [InlineData("set_profile")]
    [InlineData("set_goals")]
    public async Task SetupWrites_WithoutWriteScope_AreHiddenAndRejected(string tool)
    {
        var me = $"ds-ro-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync(tool, new Dictionary<string, object?> { ["heightCm"] = 170m }, cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == tool);
        tools.ShouldContain(t => t.Name == "get_weight_prediction");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context", [])).Profile.ShouldBeNull();
    }

    /// <summary>The assistant can run the setup tools in-process for the signed-in user.</summary>
    [Fact]
    public async Task SetupTools_InProcess()
    {
        var me = $"ds-inproc-{_tag}";
        var invoker = _mcp.Host.Services.GetRequiredService<McpToolInvoker>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", me), new Claim("person_id", TestAuthHandler.PersonIdFor(me).ToString())], "test"));

        var results = new[]
        {
            await invoker.InvokeAsync(user, "set_profile", Json(FullProfile()), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "set_goals", Json(new() { ["dailyCalorieTarget"] = 2100 }), TestContext.Current.CancellationToken),
            await invoker.InvokeAsync(user, "get_weight_prediction", Json(new() { ["dailyCalorieTarget"] = 2100m }), TestContext.Current.CancellationToken),
        };

        results.ShouldAllBe(r => r.IsError != true);
        (await _mcp.Rest(me).GetFromJsonAsync<GoalDto>("/api/v1/goals", TestContext.Current.CancellationToken))!.DailyCalorieTarget.ShouldBe(2100);
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static Dictionary<string, object?> FullProfile() => new()
    {
        ["dateOfBirth"] = "1990-04-12",
        ["gender"] = "Male",
        ["heightCm"] = 178m,
        ["currentWeightKg"] = 84m,
        ["targetWeightKg"] = 78m,
        ["activityLevel"] = "ModeratelyActive",
    };

    private static Dictionary<string, JsonElement> Json(Dictionary<string, object?> args)
        => args.ToDictionary(a => a.Key, a => JsonSerializer.SerializeToElement(a.Value));
}
