namespace Shared.Infrastructure.Web;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The admin-only authorization policy. A caller is an admin when their token carries
/// <see cref="Role"/> in its <c>roles</c> claim — emitted by the Authentik <c>roles</c> scope
/// mapping for members of the <c>home-system-admins</c> group.
/// </summary>
public static class AdminAuthorization
{
    /// <summary>Policy name for <c>RequireAuthorization(AdminAuthorization.PolicyName)</c>.</summary>
    public const string PolicyName = "Admin";

    /// <summary>Role value that grants admin access.</summary>
    public const string Role = "admin";

    /// <summary>Registers the <see cref="PolicyName"/> policy.</summary>
    /// <param name="options">The host authorization options.</param>
    /// <returns><paramref name="options"/> for chaining.</returns>
    public static AuthorizationOptions AddAdminPolicy(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // The JWT handler may keep the raw "roles" claim type or map it to ClaimTypes.Role
        // depending on MapInboundClaims; accept either so the policy doesn't hinge on that switch.
        options.AddPolicy(PolicyName, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => ctx.User.HasClaim(c =>
                (c.Type == "roles" || c.Type == ClaimTypes.Role) && c.Value == Role)));

        return options;
    }
}
