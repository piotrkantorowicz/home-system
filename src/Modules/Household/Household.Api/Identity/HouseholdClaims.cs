namespace Household.Api.Identity;

using System.Security.Claims;

/// <summary>Claim types the Household module adds to the authenticated principal.</summary>
public static class HouseholdClaims
{
    /// <summary>The caller's <c>Person.Id</c> (a GUID string).</summary>
    public const string PersonId = "person_id";

    /// <summary>The caller's active <c>Household.Id</c> (a GUID string).</summary>
    public const string HouseholdId = "household_id";

    /// <summary>The caller's <see cref="Household.Domain.ValueObjects.HouseholdRole"/> in that household.</summary>
    public const string HouseholdRole = "household_role";
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

    /// <summary>The caller's <c>PersonId</c>, or <see langword="null"/> before the context claims are attached.</summary>
    public static Guid? GetPersonId(this ClaimsPrincipal principal)
        => TryGetGuid(principal, HouseholdClaims.PersonId);

    /// <summary>The caller's active <c>HouseholdId</c>, or <see langword="null"/> when they have no household.</summary>
    public static Guid? GetHouseholdId(this ClaimsPrincipal principal)
        => TryGetGuid(principal, HouseholdClaims.HouseholdId);

    /// <summary>The caller's household role, or <see langword="null"/> when they have no household.</summary>
    public static string? GetHouseholdRole(this ClaimsPrincipal principal)
        => principal.FindFirstValue(HouseholdClaims.HouseholdRole);

    private static Guid? TryGetGuid(ClaimsPrincipal principal, string claimType)
        => Guid.TryParse(principal.FindFirstValue(claimType), out var value) ? value : null;
}
