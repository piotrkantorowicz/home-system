namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Web;

/// <summary>
/// The same tools called in-process through <see cref="McpToolInvoker"/>, without the <c>/mcp</c>
/// transport, against the real dispatchers and PostgreSQL.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpToolInvokerTests(DatabaseFixture db) : IDisposable
{
    private readonly McpTestHost _mcp = new(db.ConnectionString);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    private McpToolInvoker Invoker => _mcp.Host.Services.GetRequiredService<McpToolInvoker>();

    /// <summary>The in-process catalog is the one <c>tools/list</c> serves.</summary>
    [Fact]
    public async Task Tools_MatchListedTools()
    {
        await using var client = await _mcp.ConnectAsync($"inproc-{_tag}", string.Join(' ', DietPlannerMcp.Scopes));

        var listed = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Invoker.Tools.Select(t => JsonSerializer.Serialize(t)).ShouldBe(
            listed.OrderBy(t => t.Name, StringComparer.Ordinal).Select(t => JsonSerializer.Serialize(t.ProtocolTool)));
    }

    /// <summary>A read tool returns the same result in-process as over <c>/mcp</c> for the same caller.</summary>
    [Fact]
    public async Task InvokeAsync_ReadTool_ReturnsSameResultAsMcp()
    {
        var me = $"inproc-me-{_tag}";
        (await _mcp.Rest(me).PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest($"Oats {_tag}", 380m, null, null, null, null, "g", null, null, "Private"),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);
        var arguments = new Dictionary<string, JsonElement> { ["query"] = JsonSerializer.SerializeToElement(_tag) };

        var overMcp = await client.CallToolAsync(
            "search_products", arguments.ToDictionary(a => a.Key, a => (object?)a.Value), cancellationToken: TestContext.Current.CancellationToken);
        var inProcess = await Invoker.InvokeAsync(Caller(me), "search_products", arguments, TestContext.Current.CancellationToken);

        inProcess.IsError.ShouldNotBe(true);
        McpTestHost.Text(inProcess).ShouldContain($"Oats {_tag}");
        McpTestHost.Text(inProcess).ShouldBe(McpTestHost.Text(overMcp));
    }

    /// <summary>Expected tool errors come back as error results with the same actionable text as over <c>/mcp</c>.</summary>
    [Fact]
    public async Task InvokeAsync_CallerWithoutPerson_ReturnsSameErrorAsMcp()
    {
        var subject = $"{McpTestHouseholds.NoPersonPrefix}{_tag}";
        var caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "test"));
        await using var client = await _mcp.ConnectAsync(subject, DietPlannerMcp.ReadScope);

        var overMcp = await McpTestHost.CallExpectingErrorAsync(client, "get_diet_context");
        var inProcess = await Invoker.InvokeAsync(caller, "get_diet_context", null, TestContext.Current.CancellationToken);

        inProcess.IsError.ShouldBe(true);
        McpTestHost.Text(inProcess).ShouldContain("household");
        McpTestHost.Text(inProcess).ShouldBe(overMcp);
    }

    /// <summary>A missing required argument is an error result the model can correct, as over <c>/mcp</c>, not an exception.</summary>
    [Fact]
    public async Task InvokeAsync_MissingRequiredArgument_ReturnsSameErrorAsMcp()
    {
        var me = $"inproc-{_tag}";
        await using var client = await _mcp.ConnectAsync(me, DietPlannerMcp.ReadScope);

        var overMcp = await McpTestHost.CallExpectingErrorAsync(client, "get_product");
        var inProcess = await Invoker.InvokeAsync(Caller(me), "get_product", null, TestContext.Current.CancellationToken);

        inProcess.IsError.ShouldBe(true);
        McpTestHost.Text(inProcess).ShouldBe(overMcp);
    }

    /// <summary>An unknown tool name is an error result, not an exception.</summary>
    [Fact]
    public async Task InvokeAsync_UnknownTool_ReturnsError()
    {
        var result = await Invoker.InvokeAsync(Caller($"inproc-{_tag}"), "drop_database", null, TestContext.Current.CancellationToken);

        result.IsError.ShouldBe(true);
        McpTestHost.Text(result).ShouldContain("Unknown tool");
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static ClaimsPrincipal Caller(string subject)
        => new(new ClaimsIdentity(
            [new Claim("sub", subject), new Claim("person_id", TestAuthHandler.PersonIdFor(subject).ToString())], "test"));
}
