namespace DietPlanner.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using DietPlanner.Api;
using DietPlanner.Api.Mcp;
using DietPlanner.IntegrationTests.Infrastructure;
using Household.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shared.Infrastructure.Web;

/// <summary>
/// Integration tests for the MCP transport: SDK client → <c>/mcp</c> → JWT bearer (locally signed) →
/// household claims → scope policies → dispatcher → PostgreSQL (Testcontainers).
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpEndpointTests(DatabaseFixture db) : IDisposable
{
    private const string Issuer = "https://auth.test/application/o/home-system-mcp/";
    private const string Audience = "home-system-mcp";
    private static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "mcp-test" };
    private static readonly string[] AllMealSlotsMissing = ["profile", "goal", "meal_schedule"];

    private readonly List<IDisposable> _disposables = [];

    /// <summary>With the read scope, <c>get_diet_context</c> returns the caller's own profile, goal and slots.</summary>
    [Fact]
    public async Task GetDietContext_WithReadScope_ReturnsCallersSetup()
    {
        var subject = $"mcp-{Guid.CreateVersion7()}";
        var host = McpHost();
        var rest = RestClient(host, subject);
        (await rest.PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00"), new MealSlotRequest(null, "Dinner", "18:00")]),
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        (await rest.PostAsJsonAsync("/api/v1/goals", new { dailyCalorieTarget = 2100 }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        await using var client = await ConnectAsync(host, Token(subject, DietPlannerMcp.ReadScope));
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
        var host = McpHost();
        (await RestClient(host, first).PutAsJsonAsync("/api/v1/meal-schedule",
            new UpdateMealScheduleRequest([new MealSlotRequest(null, "Lunch", "12:00")]), TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        await using var client = await ConnectAsync(host, Token($"mcp-{Guid.CreateVersion7()}", DietPlannerMcp.ReadScope));
        var context = await GetDietContextAsync(client);

        context.MealSlots.ShouldBeEmpty();
        context.MissingSetup.ShouldBe(AllMealSlotsMissing);
    }

    /// <summary>A caller without a synced person gets an actionable tool error instead of data.</summary>
    [Fact]
    public async Task GetDietContext_WithoutPerson_ReturnsActionableError()
    {
        await using var client = await ConnectAsync(McpHost(), Token($"{McpTestHouseholds.NoPersonPrefix}{Guid.CreateVersion7()}", DietPlannerMcp.ReadScope));

        var result = await client.CallToolAsync("get_diet_context", cancellationToken: TestContext.Current.CancellationToken);

        result.IsError.ShouldBe(true);
        Text(result).ShouldContain("household");
    }

    /// <summary>A token without the read scope neither lists nor runs the tool.</summary>
    [Fact]
    public async Task GetDietContext_WithoutReadScope_IsHiddenAndRejected()
    {
        await using var client = await ConnectAsync(McpHost(), Token($"mcp-{Guid.CreateVersion7()}", "openid profile"));

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
        string? token = kind switch
        {
            "none" => null,
            "forged" => Token(subject, DietPlannerMcp.ReadScope, key: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "mcp-test" }),
            "expired" => Token(subject, DietPlannerMcp.ReadScope, expires: TimeProvider.System.GetUtcNow().UtcDateTime.AddMinutes(-10)),
            "audience" => Token(subject, DietPlannerMcp.ReadScope, audience: "home-system-spa"),
            _ => Token(subject, DietPlannerMcp.ReadScope, issuer: "https://elsewhere.test/"),
        };
        var http = McpHost().CreateClient();
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
        var http = McpHost().CreateClient();

        var metadata = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource", TestContext.Current.CancellationToken);

        metadata.GetProperty("authorization_servers")[0].GetString().ShouldBe(Issuer);
        metadata.GetProperty("scopes_supported").EnumerateArray().Select(s => s.GetString()).ShouldBe(DietPlannerMcp.Scopes);
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
        foreach (var disposable in _disposables)
            disposable.Dispose();
    }

    private T Track<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    private WebApplicationFactory<Program> McpHost()
    {
        var root = Track(new DietPlannerWebApplicationFactory(db.ConnectionString, settings: new Dictionary<string, string?>
        {
            ["Mcp:Enabled"] = "true",
            ["Authentication:Mcp:Authority"] = Issuer,
            ["Authentication:Mcp:Audience"] = Audience,
        }));

        return Track(root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHouseholdQueryService>();
            services.AddSingleton<IHouseholdQueryService, McpTestHouseholds>();

            // Validate locally signed tokens instead of fetching Authentik's discovery document.
            services.PostConfigure<JwtBearerOptions>(McpAuthorization.SchemeName, options =>
            {
                options.ConfigurationManager = null;
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            });
        })));
    }

    private HttpClient RestClient(WebApplicationFactory<Program> host, string subject)
        => Track(host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(new TestUserIdOverride(subject))))).CreateClient();

    private static async Task<McpClient> ConnectAsync(WebApplicationFactory<Program> host, string token)
    {
        var http = host.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task<DietContextDto> GetDietContextAsync(McpClient client)
    {
        var result = await client.CallToolAsync("get_diet_context", cancellationToken: TestContext.Current.CancellationToken);
        result.IsError.ShouldNotBe(true, Text(result));
        return JsonSerializer.Deserialize<DietContextDto>(Text(result), JsonSerializerOptions.Web)!;
    }

    private static string Text(CallToolResult result)
        => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static string Token(
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
}
