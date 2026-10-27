namespace DietPlanner.IntegrationTests.Mcp;

using System.Text.Json;
using System.Text.Json.Serialization;
using DietPlanner.IntegrationTests.Infrastructure;
using Household.Contracts.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shared.Testing.Mcp;

/// <summary>
/// A host with MCP enabled whose <c>McpBearer</c> scheme validates locally signed tokens, plus helpers
/// to act as one subject over REST (test scheme) and over MCP (JWT). Both resolve to the same person.
/// </summary>
/// <param name="connectionString">Connection string of the shared test database.</param>
/// <param name="configureServices">Extra service overrides, e.g. to inject a failure.</param>
/// <param name="settings">Configuration overrides on top of the MCP defaults below.</param>
internal sealed class McpTestHost(
    string connectionString,
    Action<IServiceCollection>? configureServices = null,
    IReadOnlyDictionary<string, string?>? settings = null) : IDisposable
{
    public const string Issuer = McpTestAuth.Issuer;
    public const string PublicBaseUrl = McpTestAuth.PublicBaseUrl;

    private readonly List<IDisposable> _disposables = [];
    private WebApplicationFactory<Program>? _host;

    /// <summary>Household lookup the host uses; add entries to put subjects in one household.</summary>
    public McpTestHouseholds Households { get; } = new();

    public WebApplicationFactory<Program> Host => _host ??= Build();

    /// <summary>An HTTP client addressing the host by its public URL, as a real MCP client does.</summary>
    public HttpClient CreateClient() => Host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(PublicBaseUrl) });

    public HttpClient Rest(string subject)
        => Track(Host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(new TestUserIdOverride(subject))))).CreateClient();

    public Task<McpClient> ConnectAsync(string subject, string scope)
        => McpTestClient.ConnectAsync(CreateClient, subject, scope, TestContext.Current.CancellationToken);

    // The MCP SDK writes enums as names (e.g. "Monday"); the converter also reads numbers.
    private static readonly JsonSerializerOptions ResultJson = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task<T> CallAsync<T>(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldNotBe(true, Text(result));
        return JsonSerializer.Deserialize<T>(Text(result), ResultJson)!;
    }

    public static async Task<string> CallExpectingErrorAsync(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldBe(true);
        return Text(result);
    }

    public static string Text(CallToolResult result) => McpTestClient.Text(result);

    public static string Token(
        string subject,
        string scope,
        SecurityKey? key = null,
        DateTime? expires = null,
        string audience = McpTestAuth.Audience,
        string issuer = McpTestAuth.Issuer)
        => McpTestAuth.Token(subject, scope, key, expires, audience, issuer);

    public void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
    }

    private T Track<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    private WebApplicationFactory<Program> Build()
    {
        var effective = new Dictionary<string, string?>(McpTestAuth.Settings) { ["Mcp:ImportsEnabled"] = "true" };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            effective[key] = value;

        var root = Track(new DietPlannerWebApplicationFactory(connectionString, settings: effective));

        return Track(root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddTestMcpAuth();
            services.RemoveAll<IHouseholdQueryService>();
            services.AddSingleton<IHouseholdQueryService>(Households);

            configureServices?.Invoke(services);
        })));
    }
}
