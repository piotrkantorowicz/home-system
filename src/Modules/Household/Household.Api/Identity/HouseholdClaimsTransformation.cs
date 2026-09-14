namespace Household.Api.Identity;

using System.Security.Claims;
using Household.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

/// <summary>
/// Attaches <c>person_id</c>, <c>household_id</c> and <c>household_role</c> claims to every
/// authenticated principal by resolving the caller's household through
/// <see cref="IHouseholdQueryService"/>. Endpoints then read household context the same way
/// they read <c>NameIdentifier</c>.
/// </summary>
/// <remarks>
/// ASP.NET may invoke <see cref="TransformAsync"/> more than once per request, so it is
/// idempotent — it no-ops once the claims are present. A caller with no <c>Person</c> or no
/// household simply keeps the un-augmented principal, and so does the request if the
/// Household store is unreachable — this runs on every authenticated request in every
/// module, so it must never turn a transient Household outage into a 500 elsewhere.
/// </remarks>
internal sealed partial class HouseholdClaimsTransformation : IClaimsTransformation
{
    private readonly IHouseholdQueryService _households;
    private readonly ILogger<HouseholdClaimsTransformation> _logger;

    public HouseholdClaimsTransformation(
        IHouseholdQueryService households,
        ILogger<HouseholdClaimsTransformation> logger)
        => (_households, _logger) = (households, logger);

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return principal;

        if (identity.HasClaim(c => c.Type == HouseholdClaims.PersonId))
            return principal;

        var authSubject = principal.GetAuthSubject();
        if (string.IsNullOrWhiteSpace(authSubject))
            return principal;

        HouseholdContext? context;
        try
        {
            context = await _households.GetHouseholdContextForUserAsync(authSubject);
        }
        catch (Exception ex)
        {
            LogContextUnresolved(ex);
            return principal;
        }

        if (context is null)
            return principal;

        identity.AddClaim(new Claim(HouseholdClaims.PersonId, context.PersonId.ToString()));
        identity.AddClaim(new Claim(HouseholdClaims.HouseholdId, context.HouseholdId.ToString()));
        identity.AddClaim(new Claim(HouseholdClaims.HouseholdRole, context.Role));

        return principal;
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Warning,
        Message = "Could not resolve household context for the current principal; proceeding without household claims.")]
    private partial void LogContextUnresolved(Exception exception);
}
