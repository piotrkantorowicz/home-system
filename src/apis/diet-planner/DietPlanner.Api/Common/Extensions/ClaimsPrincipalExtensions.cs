using System.Security.Claims;

namespace DietPlanner.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the 'sub' claim (subject claim from JWT)
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    /// <summary>
    /// Gets the user's email from the token
    /// </summary>
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
               ?? principal.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the user's display name from the token
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value
               ?? principal.FindFirst("preferred_username")?.Value
               ?? principal.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Checks if the current user owns a resource
    /// </summary>
    public static bool OwnsResource(this ClaimsPrincipal principal, string createdByUserId)
    {
        var userId = principal.GetUserId();
        return userId == createdByUserId;
    }
}
