namespace HomeSystem.REST.Mcp;

using System.Security.Claims;
using System.Threading.RateLimiting;
using DietPlanner.Api.Mcp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using Shared.Infrastructure.Web;

/// <summary>
/// Composes the MCP endpoint from the modules' tools plus the operational controls: own token scheme,
/// grants and switches, pinned public URL, Origin check, per-caller rate limit and body size limit.
/// Design: <c>docs/design/mcp/AUTH.md</c>, operations: <c>docs/design/mcp/OPERATIONS.md</c>.
/// </summary>
internal static class McpHosting
{
    private const string RateLimitPolicy = "mcp";

    public static void AddHomeSystemMcp(this WebApplicationBuilder builder, AuthenticationBuilder authentication)
    {
        var options = builder.Configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();
        builder.Services.AddSingleton(options);
        if (!options.Enabled)
            return;

        var publicBaseUrl = options.PublicBaseUrl is { IsAbsoluteUri: true } url
            ? url
            : throw new InvalidOperationException("Mcp:PublicBaseUrl must be an absolute URL when Mcp:Enabled is true.");

        // MCP tokens come from their own OAuth client: own issuer and audience, own scheme. SPA tokens
        // cannot reach /mcp and MCP tokens cannot reach REST. Challenges go to the MCP metadata handler
        // so a 401 points the client at the resource metadata (RFC 9728) under the pinned public URL.
        var mcpAuthConfig = builder.Configuration.GetSection("Authentication:Mcp");
        authentication
            .AddJwtBearer(McpAuthorization.SchemeName, jwt =>
            {
                jwt.Authority = mcpAuthConfig["Authority"];
                jwt.Audience = mcpAuthConfig["Audience"];
                jwt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                jwt.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            })
            .AddMcp(mcp =>
            {
                mcp.ResourceMetadataUri = new Uri(publicBaseUrl, "/.well-known/oauth-protected-resource");
                mcp.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = new Uri(publicBaseUrl, "/mcp").ToString(),
                    AuthorizationServers = { mcpAuthConfig["Authority"]! },
                    ScopesSupported = [.. DietPlannerMcp.Scopes],
                    ResourceName = "Home System",
                };
            });

        // Switches act on the grants: a disabled grant hides its tools and rejects calls, for every client.
        builder.Services.AddAuthorization(authorization =>
        {
            authorization.AddMcpEndpointPolicy();
            authorization.AddMcpScopePolicy(DietPlannerMcp.ReadScope);
            authorization.AddMcpScopePolicy(DietPlannerMcp.WriteScope, enabled: options.WritesEnabled);
            authorization.AddMcpScopePolicy(DietPlannerMcp.ImportScope, enabled: options.WritesEnabled && options.ImportsEnabled);
        });

        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub")
                    ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.RequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        if (options.DataProtectionKeysPath is { Length: > 0 } keysPath)
            builder.Services.AddDataProtection().SetApplicationName("HomeSystem").PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        builder.Services.AddMcpServer()
            .WithHttpTransport(transport => transport.Stateless = true)
            .AddAuthorizationFilters()
            .WithRequestFilters(filters => filters.AddCallToolFilter(McpCallLogging.Filter))
            .AddDietPlannerMcpTools();
    }

    public static void MapHomeSystemMcp(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<McpOptions>();
        if (!options.Enabled)
            return;

        app.UseRateLimiter();

        app.UseWhen(context => context.Request.Path.StartsWithSegments("/mcp"), branch => branch.Use(async (context, next) =>
        {
            // Browsers send Origin; native MCP clients do not. Reject unknown browser origins (DNS rebinding).
            var origin = context.Request.Headers.Origin.ToString();
            if (origin.Length > 0 && !options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // Declared bodies are checked here; the server enforces the same cap on chunked bodies.
            if (context.Request.ContentLength > options.MaxRequestBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } bodySize)
                bodySize.MaxRequestBodySize = options.MaxRequestBytes;

            await next(context);
        }));

        app.MapMcp("/mcp")
            .RequireAuthorization(McpAuthorization.EndpointPolicy)
            .RequireRateLimiting(RateLimitPolicy);
    }
}
