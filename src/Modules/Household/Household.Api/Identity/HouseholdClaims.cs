namespace Household.Api.Identity;

using System.Security.Claims;

/// <summary>Claim types the Household module adds to the authenticated principal.</summary>
public static class HouseholdClaims
{
    /// <summary>
    /// The caller's <c>Person.Id</c> (a GUID string). Populated by the household-context
    /// middleware added in #219 (alongside <c>household_id</c> / <c>household_role</c>).
    /// </summary>
    public const string PersonId = "person_id";
}

internal static class ClaimsPrincipalExtensions
{
    public static string? GetAuthSubject(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? principal.FindFirstValue("sub");

    public static string? GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
           ?? principal.FindFirstValue("email");

    public static string? GetPicture(this ClaimsPrincipal principal)
        => principal.FindFirstValue("picture");

    public static string GetDisplayName(this ClaimsPrincipal principal)
        => principal.FindFirstValue("name")
           ?? principal.FindFirstValue("preferred_username")
           ?? principal.FindFirstValue(ClaimTypes.Name)
           ?? principal.GetAuthSubject()
           ?? "Unknown";
}
