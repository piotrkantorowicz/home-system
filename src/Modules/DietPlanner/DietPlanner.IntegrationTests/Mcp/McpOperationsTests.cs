namespace DietPlanner.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.Api.Mcp;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

/// <summary>Integration tests for MCP operational controls: switches, Origin check, rate and size limits, pinned metadata URLs and redacted logs.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpOperationsTests(DatabaseFixture db)
{
    private static readonly string AllScopes = $"{DietPlannerMcp.ReadScope} {DietPlannerMcp.WriteScope} {DietPlannerMcp.ImportScope}";
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    /// <summary>With imports switched off, preview still works but execution is hidden and refused, whatever the token grants.</summary>
    [Fact]
    public async Task ImportsDisabled_HidesAndRefusesExecution()
    {
        using var host = new McpTestHost(db.ConnectionString, settings: new Dictionary<string, string?> { ["Mcp:ImportsEnabled"] = "false" });
        var me = $"ops-noimport-{_tag}";
        await SetLunchAsync(host, me);
        await using var client = await host.ConnectAsync(me, AllScopes);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var preview = await McpTestHost.CallAsync<ImportPreviewResultDto>(client, "preview_plan_import", new() { ["plan"] = Plan() });
        var refused = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync("execute_plan_import",
            new Dictionary<string, object?> { ["plan"] = Plan(), ["previewToken"] = preview.PreviewToken, ["operationKey"] = Guid.CreateVersion7().ToString() },
            cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == "execute_plan_import");
        tools.ShouldContain(t => t.Name == "create_product");
        preview.Preview.CanExecute.ShouldBeTrue();
        refused.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <summary>With writes switched off, catalog writes and imports are refused while reads keep working.</summary>
    [Fact]
    public async Task WritesDisabled_RefusesWritesButServesReads()
    {
        using var host = new McpTestHost(db.ConnectionString, settings: new Dictionary<string, string?> { ["Mcp:WritesEnabled"] = "false" });
        await using var client = await host.ConnectAsync($"ops-nowrite-{_tag}", AllScopes);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var refused = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync("create_product",
            new Dictionary<string, object?> { ["operationKey"] = Guid.CreateVersion7().ToString(), ["name"] = $"Nope {_tag}", ["unit"] = "g" },
            cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.Select(t => t.Name).ShouldNotContain("create_product");
        tools.Select(t => t.Name).ShouldNotContain("execute_plan_import");
        tools.Select(t => t.Name).ShouldContain("search_products");
        refused.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <summary>Unknown browser origins are rejected; allowed origins and requests without Origin pass.</summary>
    [Theory]
    [InlineData("https://evil.test", HttpStatusCode.Forbidden)]
    [InlineData("https://app.test", HttpStatusCode.OK)]
    [InlineData(null, HttpStatusCode.OK)]
    public async Task Origin_IsValidated(string? origin, HttpStatusCode expected)
    {
        using var host = new McpTestHost(db.ConnectionString, settings: new Dictionary<string, string?> { ["Mcp:AllowedOrigins:0"] = "https://app.test" });
        var http = Authorized(host, $"ops-origin-{_tag}");
        if (origin is not null)
            http.DefaultRequestHeaders.Add("Origin", origin);

        var response = await http.SendAsync(ToolsList(), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expected);
    }

    /// <summary>A missing auth setting or a non-positive limit stops startup instead of breaking the first call.</summary>
    [Theory]
    [InlineData("Authentication:Mcp:Authority", "")]
    [InlineData("Authentication:Mcp:Audience", " ")]
    [InlineData("Mcp:RequestsPerMinute", "0")]
    [InlineData("Mcp:MaxRequestBytes", "-1")]
    public void Startup_WithInvalidSetting_Fails(string key, string value)
    {
        using var host = new McpTestHost(db.ConnectionString, settings: new Dictionary<string, string?> { [key] = value });

        Should.Throw<InvalidOperationException>(() => host.CreateClient()).Message.ShouldContain(key.Split(':')[^1]);
    }

    /// <summary>A caller over the per-minute limit gets 429.</summary>
    [Fact]
    public async Task RateLimit_RejectsCallsOverTheLimit()
    {
        using var host = new McpTestHost(db.ConnectionString, settings: new Dictionary<string, string?> { ["Mcp:RequestsPerMinute"] = "3" });
        var http = Authorized(host, $"ops-rate-{_tag}");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            statuses.Add((await http.SendAsync(ToolsList(), TestContext.Current.CancellationToken)).StatusCode);

        statuses.ShouldBe([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests]);
    }

    /// <summary>An oversized request body is refused before any tool runs.</summary>
    [Fact]
    public async Task OversizedRequest_IsRefused()
    {
        using var host = new McpTestHost(db.ConnectionString, settings: new Dictionary<string, string?> { ["Mcp:MaxRequestBytes"] = "2048" });
        var http = Authorized(host, $"ops-size-{_tag}");
        var request = ToolsList(padding: new string('x', 4096));

        var response = await http.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    /// <summary>Resource metadata and challenges come from the configured public URL; a forged Host or forwarded header cannot rewrite them.</summary>
    [Fact]
    public async Task MetadataUrls_IgnoreHostAndForwardedHeaders()
    {
        using var host = new McpTestHost(db.ConnectionString);
        var forged = host.CreateClient();
        forged.DefaultRequestHeaders.Add("X-Forwarded-Host", "evil.test");
        forged.DefaultRequestHeaders.Add("X-Forwarded-Proto", "http");

        var metadata = await forged.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource", TestContext.Current.CancellationToken);
        forged.DefaultRequestHeaders.Host = "evil.test";
        var spoofedHost = await forged.GetAsync("/.well-known/oauth-protected-resource", TestContext.Current.CancellationToken);
        var challenge = await forged.SendAsync(ToolsList(), TestContext.Current.CancellationToken);

        metadata.GetProperty("resource").GetString().ShouldBe($"{McpTestHost.PublicBaseUrl}/mcp");
        spoofedHost.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        challenge.Headers.WwwAuthenticate.ToString().ShouldContain($"{McpTestHost.PublicBaseUrl}/.well-known/oauth-protected-resource");
        challenge.Headers.WwwAuthenticate.ToString().ShouldNotContain("evil.test");
    }

    /// <summary>Tool calls are logged with name, outcome and duration, never with the plan, its notes or the token.</summary>
    [Fact]
    public async Task ToolCallLogs_AreRedacted()
    {
        var logs = new CapturingLoggerProvider();
        using var host = new McpTestHost(db.ConnectionString, services => services.AddSingleton<ILoggerProvider>(logs));
        var me = $"ops-logs-{_tag}";
        await SetLunchAsync(host, me);
        await using var client = await host.ConnectAsync(me, AllScopes);
        var plan = Plan(notes: $"SECRET-NOTE-{_tag}");

        var preview = await McpTestHost.CallAsync<ImportPreviewResultDto>(client, "preview_plan_import", new() { ["plan"] = plan });
        await McpTestHost.CallAsync<JsonElement>(client, "execute_plan_import", new()
        {
            ["plan"] = plan,
            ["previewToken"] = preview.PreviewToken,
            ["operationKey"] = Guid.CreateVersion7().ToString(),
        });

        var lines = logs.Lines.ToList();
        lines.ShouldContain(l => l.Contains("MCP tool execute_plan_import finished with ok", StringComparison.Ordinal));
        lines.ShouldNotContain(l => l.Contains("SECRET-NOTE", StringComparison.Ordinal));
        lines.ShouldNotContain(l => l.Contains(preview.PreviewToken!, StringComparison.Ordinal));
    }

    private ImportDto Plan(string? notes = null)
        => new(
            [new ImportProductDto($"Bread {_tag}", 250m, 9m, 49m, 3m, 3m, "g", null, null)],
            [new ImportRecipeDto($"Toast {_tag}", null, null, 1, null, [new ImportIngredientDto($"Bread {_tag}", 60m, "g")])],
            [new ImportScheduleDto("2026-10-01", [new ImportMealDto("Lunch", $"Toast {_tag}", 1m, notes)])]);

    private static async Task SetLunchAsync(McpTestHost host, string subject)
        => (await host.Rest(subject).PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Lunch", "12:00")]), TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

    private static HttpClient Authorized(McpTestHost host, string subject)
    {
        var http = host.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", McpTestHost.Token(subject, DietPlannerMcp.ReadScope));
        return http;
    }

    private static HttpRequestMessage ToolsList(string? padding = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "tools/list", @params = new { padding } }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }
}
