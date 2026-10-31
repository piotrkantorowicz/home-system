namespace DietPlanner.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;
using ModelContextProtocol.Client;

/// <summary>
/// Integration tests for the MCP transport: SDK client → <c>/mcp</c> → JWT bearer (locally signed) →
/// household claims → scope policies → dispatcher → PostgreSQL (Testcontainers).
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpEndpointTests(DatabaseFixture db) : IDisposable
{
    private static readonly string[] AllMealSlotsMissing = ["profile", "goal", "meal_schedule"];

    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly List<IDisposable> _disposables = [];

    /// <summary>With the read scope, <c>get_diet_context</c> returns the caller's own profile, goal and slots.</summary>
    [Fact]
    public async Task GetDietContext_WithReadScope_ReturnsCallersSetup()
    {
        var subject = $"mcp-{Guid.CreateVersion7()}";
        var rest = _mcp.Rest(subject);
        (await rest.PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00"), new MealSlotRequest(null, "Dinner", "18:00")]),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await rest.PostAsJsonAsync("/api/v1/goals", new { dailyCalorieTarget = 2100 }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        await using var client = await _mcp.ConnectAsync(subject, DietPlannerMcp.ReadScope);
        var context = await GetDietContextAsync(client);

        context.MealSlots.Select(s => s.Name).ShouldBe(["Breakfast", "Dinner"]);
        context.Goal.ShouldNotBeNull().DailyCalorieTarget.ShouldBe(2100);
        context.MissingSetup.ShouldBe(["profile"]);
    }

    /// <summary>Two identities each see only their own context.</summary>
    [Fact]
    public async Task GetDietContext_ForSecondIdentity_DoesNotSeeFirstIdentitysSetup()
    {
        var first = $"mcp-{Guid.CreateVersion7()}";
        (await _mcp.Rest(first).PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Lunch", "12:00")]), TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        await using var client = await _mcp.ConnectAsync($"mcp-{Guid.CreateVersion7()}", DietPlannerMcp.ReadScope);
        var context = await GetDietContextAsync(client);

        context.MealSlots.ShouldBeEmpty();
        context.MissingSetup.ShouldBe(AllMealSlotsMissing);
    }

    /// <summary>A caller without a synced person gets an actionable tool error instead of data.</summary>
    [Fact]
    public async Task GetDietContext_WithoutPerson_ReturnsActionableError()
    {
        await using var client = await _mcp.ConnectAsync($"{McpTestHouseholds.NoPersonPrefix}{Guid.CreateVersion7()}", DietPlannerMcp.ReadScope);

        var result = await client.CallToolAsync("get_diet_context", cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.ShouldBe(true);
        McpTestHost.Text(result).ShouldContain("household");
    }

    /// <summary>A token without the read scope neither lists nor runs the tool.</summary>
    [Fact]
    public async Task GetDietContext_WithoutReadScope_IsHiddenAndRejected()
    {
        await using var client = await _mcp.ConnectAsync($"mcp-{Guid.CreateVersion7()}", "openid profile");

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync("get_diet_context", cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == "get_diet_context");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <summary>Missing, forged, expired and wrong-audience tokens are rejected with a challenge naming the resource metadata.</summary>
    [Theory]
    [InlineData("none")]
    [InlineData("forged")]
    [InlineData("expired")]
    [InlineData("audience")]
    [InlineData("issuer")]
    public async Task PostMcp_WithInvalidToken_Returns401WithResourceMetadata(string kind)
    {
        var subject = $"mcp-{Guid.CreateVersion7()}";
        using var forgedKey = RSA.Create(2048);
        string? token = kind switch
        {
            "none" => null,
            "forged" => McpTestHost.Token(subject, DietPlannerMcp.ReadScope, key: new RsaSecurityKey(forgedKey) { KeyId = "mcp-test" }),
            "expired" => McpTestHost.Token(subject, DietPlannerMcp.ReadScope, expires: TimeProvider.System.GetUtcNow().UtcDateTime.AddMinutes(-10)),
            "audience" => McpTestHost.Token(subject, DietPlannerMcp.ReadScope, audience: "home-system-spa"),
            _ => McpTestHost.Token(subject, DietPlannerMcp.ReadScope, issuer: "https://elsewhere.test/"),
        };
        var http = _mcp.CreateClient();
        if (token is not null)
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldContain("resource_metadata");
    }

    /// <summary>The protected resource metadata is the only anonymous MCP endpoint and names the issuer and scopes.</summary>
    [Fact]
    public async Task GetProtectedResourceMetadata_Anonymously_DescribesAuthorizationServerAndScopes()
    {
        var http = _mcp.CreateClient();

        var metadata = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource", TestContext.Current.CancellationToken);

        metadata.GetProperty("authorization_servers")[0].GetString().ShouldBe(McpTestHost.Issuer);
        DietPlannerMcp.Scopes.ShouldBeSubsetOf(metadata.GetProperty("scopes_supported").EnumerateArray().Select(s => s.GetString()));
    }

    /// <summary>With MCP disabled (the default), the endpoint does not exist.</summary>
    [Fact]
    public async Task PostMcp_WhenDisabled_Returns404()
    {
        var host = Track(new DietPlannerWebApplicationFactory(db.ConnectionString));

        var response = await host.CreateClient().PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mcp.Dispose();
        foreach (var disposable in _disposables)
            disposable.Dispose();
    }

    private T Track<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    private static Task<DietContextDto> GetDietContextAsync(McpClient client)
        => McpTestHost.CallAsync<DietContextDto>(client, "get_diet_context");
}
