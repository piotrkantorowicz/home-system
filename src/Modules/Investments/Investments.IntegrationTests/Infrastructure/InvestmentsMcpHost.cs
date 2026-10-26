namespace Investments.IntegrationTests.Infrastructure;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shared.Infrastructure.Web;

/// <summary>
/// <see cref="InvestmentsApiFactory"/> with MCP enabled and its <c>McpBearer</c> scheme validating locally
/// signed tokens, so one subject can act over REST (test headers) and over MCP (JWT) on the same databases.
/// </summary>
/// <param name="factory">The REST host whose databases and fakes the MCP host shares.</param>
internal sealed class InvestmentsMcpHost(InvestmentsApiFactory factory) : IDisposable
{
    private const string Issuer = "https://auth.test/application/o/home-system-mcp/";
    private const string Audience = "home-system-mcp";
    private const string PublicBaseUrl = "https://mcp.test";
    private static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "mcp-test" };

    private readonly WebApplicationFactory<Program> _host = factory.WithWebHostBuilder(builder =>
    {
        builder.UseSetting("Mcp:Enabled", "true");
        builder.UseSetting("Mcp:PublicBaseUrl", PublicBaseUrl);
        builder.UseSetting("Authentication:Mcp:Authority", Issuer);
        builder.UseSetting("Authentication:Mcp:Audience", Audience);
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(McpAuthorization.SchemeName, options =>
            {
                // Validate locally signed tokens instead of fetching Authentik's discovery document.
                options.ConfigurationManager = null;
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            }));
    });

    /// <summary>The subject a REST client from <see cref="InvestmentsApiFactory.CreateClientFor"/> acts as.</summary>
    public static string Subject(HttpClient rest) => rest.DefaultRequestHeaders.GetValues("X-Test-Sub").Single();

    public async Task<McpClient> ConnectAsync(string subject, string scope)
    {
        var http = _host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(PublicBaseUrl) });
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(subject, scope));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    public static async Task<string> CallAsync(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldNotBe(true, Text(result));
        return Text(result);
    }

    public static async Task<string> CallExpectingErrorAsync(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldBe(true);
        return Text(result);
    }

    /// <summary>Re-serialises <paramref name="json"/> through <typeparamref name="T"/> so REST and MCP payloads compare field by field.</summary>
    public static string Normalize<T>(string json)
        => JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web), JsonSerializerOptions.Web);

    public void Dispose() => _host.Dispose();

    private static string Text(CallToolResult result)
        => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static string Token(string subject, string scope)
    {
        var expiry = TimeProvider.System.GetUtcNow().UtcDateTime.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([new Claim("sub", subject), new Claim("scope", scope)]),
            NotBefore = expiry.AddMinutes(-20),
            IssuedAt = expiry.AddMinutes(-20),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256),
        });
    }
}
