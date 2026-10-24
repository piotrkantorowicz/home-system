namespace DietPlanner.IntegrationTests.Mcp;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using DietPlanner.IntegrationTests.Infrastructure;
using Household.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shared.Infrastructure.Web;

/// <summary>
/// A host with MCP enabled whose <c>McpBearer</c> scheme validates locally signed tokens, plus helpers
/// to act as one subject over REST (test scheme) and over MCP (JWT). Both resolve to the same person.
/// </summary>
/// <param name="connectionString">Connection string of the shared test database.</param>
internal sealed class McpTestHost(string connectionString) : IDisposable
{
    public const string Issuer = "https://auth.test/application/o/home-system-mcp/";
    public const string Audience = "home-system-mcp";
    public static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "mcp-test" };

    private readonly List<IDisposable> _disposables = [];
    private WebApplicationFactory<Program>? _host;

    /// <summary>Household lookup the host uses; add entries to put subjects in one household.</summary>
    public McpTestHouseholds Households { get; } = new();

    public WebApplicationFactory<Program> Host => _host ??= Build();

    public HttpClient Rest(string subject)
        => Track(Host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(new TestUserIdOverride(subject))))).CreateClient();

    public async Task<McpClient> ConnectAsync(string subject, string scope)
    {
        var http = Host.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(subject, scope));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    public static async Task<T> CallAsync<T>(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldNotBe(true, Text(result));
        return JsonSerializer.Deserialize<T>(Text(result), JsonSerializerOptions.Web)!;
    }

    public static async Task<string> CallExpectingErrorAsync(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldBe(true);
        return Text(result);
    }

    public static string Text(CallToolResult result)
        => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    public static string Token(
        string subject,
        string scope,
        SecurityKey? key = null,
        DateTime? expires = null,
        string audience = Audience,
        string issuer = Issuer)
    {
        var expiry = expires ?? TimeProvider.System.GetUtcNow().UtcDateTime.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", subject), new Claim("scope", scope)]),
            NotBefore = expiry.AddMinutes(-20),
            IssuedAt = expiry.AddMinutes(-20),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.RsaSha256),
        });
    }

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
        var root = Track(new DietPlannerWebApplicationFactory(connectionString, settings: new Dictionary<string, string?>
        {
            ["Mcp:Enabled"] = "true",
            ["Authentication:Mcp:Authority"] = Issuer,
            ["Authentication:Mcp:Audience"] = Audience,
        }));

        return Track(root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHouseholdQueryService>();
            services.AddSingleton<IHouseholdQueryService>(Households);

            // Validate locally signed tokens instead of fetching Authentik's discovery document.
            services.PostConfigure<JwtBearerOptions>(McpAuthorization.SchemeName, options =>
            {
                options.ConfigurationManager = null;
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            });
        })));
    }
}
