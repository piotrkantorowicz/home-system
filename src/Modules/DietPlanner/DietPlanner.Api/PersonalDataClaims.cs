namespace DietPlanner.Api;

using System.Security.Claims;
using Shared.Abstractions.Core.Domain;

internal static class PersonalDataClaims
{
    internal static Guid GetPersonId(ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue("person_id"), out Guid personId) && personId != Guid.Empty
            ? personId
            : throw new ForbiddenException("Sync your Person and join or create a household before using DietPlanner personal data.");

    internal static string GetAuthSubject(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
