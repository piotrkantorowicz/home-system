namespace Investments.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

internal static class InvestmentsEndpoints
{
    internal static IEndpointRouteBuilder MapInvestmentsEndpointsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/investments")
            .RequireAuthorization()
            .WithTags("Investments");

        // Catch-all so the whole prefix answers 401 to anonymous callers and 404 to signed-in ones
        // for routes that do not exist; real endpoints always win over a fallback.
        group.MapFallback(() => TypedResults.NotFound())
            .ExcludeFromDescription();

        return app;
    }
}
