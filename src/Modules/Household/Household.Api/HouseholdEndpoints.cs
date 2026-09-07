namespace Household.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

internal static class HouseholdEndpoints
{
    // Endpoints are added in #217 (commands, queries). This group exists so the module is
    // wired into the host pipeline from the scaffold onward.
    internal static IEndpointRouteBuilder MapHouseholdEndpointsGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/households")
            .RequireAuthorization()
            .WithTags("Households");

        return app;
    }
}
