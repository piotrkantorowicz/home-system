namespace Shared.Infrastructure.Web;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization for the MCP transport. MCP tokens come from their own OAuth client and are
/// accepted only by the <see cref="SchemeName"/> scheme, so SPA tokens cannot call tools and MCP
/// tokens cannot call REST. Each grant is an OAuth scope, enforced server-side per tool.
/// </summary>
public static class McpAuthorization
{
    /// <summary>JWT bearer scheme that validates MCP access tokens (issuer and audience of the MCP client).</summary>
    public const string SchemeName = "McpBearer";

    /// <summary>Policy for the <c>/mcp</c> endpoint itself: any caller authenticated by <see cref="SchemeName"/>.</summary>
    public const string EndpointPolicy = "Mcp";

    /// <summary>Registers <see cref="EndpointPolicy"/>.</summary>
    /// <param name="options">The authorization options to extend.</param>
    /// <returns><paramref name="options"/> for chaining.</returns>
    public static AuthorizationOptions AddMcpEndpointPolicy(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(EndpointPolicy, policy => policy
            .AddAuthenticationSchemes(SchemeName)
            .RequireAuthenticatedUser());

        return options;
    }

    /// <summary>Registers a policy named after <paramref name="scope"/> that requires an MCP token granting it.</summary>
    /// <param name="options">The authorization options to extend.</param>
    /// <param name="scope">The OAuth scope, e.g. <c>mcp:diet:read</c>; also the policy name.</param>
    /// <returns><paramref name="options"/> for chaining.</returns>
    public static AuthorizationOptions AddMcpScopePolicy(this AuthorizationOptions options, string scope)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(scope, policy => policy
            .AddAuthenticationSchemes(SchemeName)
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => HasScope(ctx.User, scope)));

        return options;
    }

    /// <summary>True when the token grants <paramref name="scope"/>.</summary>
    /// <param name="user">The authenticated caller.</param>
    /// <param name="scope">The scope to look for.</param>
    /// <returns>Whether the space-separated <c>scope</c> claim (Authentik) contains it.</returns>
    public static bool HasScope(ClaimsPrincipal user, string scope)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.Claims
            .Where(c => c.Type == "scope")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope, StringComparer.Ordinal);
    }
}
