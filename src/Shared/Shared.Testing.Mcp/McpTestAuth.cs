namespace Shared.Testing.Mcp;

using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Web;

/// <summary>
/// MCP settings and a locally signed bearer token for integration tests, so a test host validates MCP tokens
/// without Authentik.
/// </summary>
public static class McpTestAuth
{
    /// <summary>The token issuer the test host trusts.</summary>
    public const string Issuer = "https://auth.test/application/o/home-system-mcp/";

    /// <summary>The token audience the test host expects.</summary>
    public const string Audience = "home-system-mcp";

    /// <summary>The public base URL of the MCP endpoint.</summary>
    public const string PublicBaseUrl = "https://mcp.test";

    /// <summary>The key test tokens are signed with.</summary>
    public static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "mcp-test" };

    /// <summary>Configuration that enables MCP and points its bearer scheme at the test issuer.</summary>
    public static IReadOnlyDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>
    {
        ["Mcp:Enabled"] = "true",
        ["Mcp:PublicBaseUrl"] = PublicBaseUrl,
        ["Authentication:Mcp:Authority"] = Issuer,
        ["Authentication:Mcp:Audience"] = Audience,
    };

    /// <summary>
    /// Makes the <c>McpBearer</c> scheme validate tokens signed with <see cref="SigningKey"/> instead of fetching
    /// Authentik's discovery document, and arms <see cref="Mcp499Probe"/>.
    /// </summary>
    public static void AddTestMcpAuth(this IServiceCollection services)
    {
        services.AddTransient<IStartupFilter, Mcp499Probe>();
        services.PostConfigure<JwtBearerOptions>(McpAuthorization.SchemeName, options =>
        {
            options.ConfigurationManager = null;
            options.TokenValidationParameters.ValidIssuer = Issuer;
            options.TokenValidationParameters.IssuerSigningKey = SigningKey;
        });
    }

    /// <summary>A bearer token for <paramref name="subject"/> with <paramref name="scope"/>, valid for ten minutes by default.</summary>
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
}
