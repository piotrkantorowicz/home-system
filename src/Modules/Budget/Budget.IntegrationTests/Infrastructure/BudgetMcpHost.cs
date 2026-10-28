namespace Budget.IntegrationTests.Infrastructure;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using ModelContextProtocol.Client;
using Shared.Testing.Mcp;

/// <summary>
/// <see cref="BudgetApiFactory"/> with MCP enabled and its <c>McpBearer</c> scheme validating locally
/// signed tokens, so one subject can act over REST (test headers) and over MCP (JWT) on the same databases.
/// </summary>
/// <param name="factory">The REST host whose databases the MCP host shares.</param>
/// <param name="importsEnabled">Whether <c>Mcp:ImportsEnabled</c> is on; off in production by default.</param>
internal sealed class BudgetMcpHost(BudgetApiFactory factory, bool importsEnabled = true) : IDisposable
{
    private readonly WebApplicationFactory<Program> _host = factory.WithWebHostBuilder(builder =>
    {
        foreach (var (key, value) in McpTestAuth.Settings)
            builder.UseSetting(key, value);
        builder.UseSetting("Mcp:ImportsEnabled", importsEnabled ? "true" : "false");
        builder.ConfigureTestServices(services => services.AddTestMcpAuth());
    });

    /// <summary>The MCP-enabled host's services, for calling tools in-process.</summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>The subject a REST client from <see cref="BudgetApiFactory.CreateClientFor"/> acts as.</summary>
    public static string Subject(HttpClient rest) => rest.DefaultRequestHeaders.GetValues("X-Test-Sub").Single();

    public Task<McpClient> ConnectAsync(string subject, string scope)
        => McpTestClient.ConnectAsync(
            () => _host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(McpTestAuth.PublicBaseUrl) }),
            subject,
            scope,
            TestContext.Current.CancellationToken);

    public static async Task<string> CallAsync(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldNotBe(true, McpTestClient.Text(result));
        return McpTestClient.Text(result);
    }

    public static async Task<string> CallExpectingErrorAsync(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldBe(true);
        return McpTestClient.Text(result);
    }

    /// <summary>Re-serialises <paramref name="json"/> through <typeparamref name="T"/> so REST and MCP payloads compare field by field.</summary>
    public static string Normalize<T>(string json)
        => JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web), JsonSerializerOptions.Web);

    public void Dispose() => _host.Dispose();
}
