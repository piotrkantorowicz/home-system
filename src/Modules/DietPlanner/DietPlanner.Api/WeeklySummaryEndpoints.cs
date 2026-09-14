namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Queries.GetWeeklySummary;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoint for an on-demand weekly summary (<c>/api/v1/weekly-summary</c>).
/// </summary>
public static class WeeklySummaryEndpoints
{
    /// <summary>Maps the weekly summary query; requires an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapWeeklySummaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/weekly-summary")
            .WithTags("WeeklySummary")
            .RequireAuthorization();

        group.MapGet("/", GetWeeklySummary)
            .WithName("GetWeeklySummary")
            .WithSummary("Get the weekly diet summary for the current user")
            .WithDescription("Returns aggregated nutrition and hydration statistics for the specified week. Returns zeros for weeks with no data.")
            .Produces<WeeklySummaryDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetWeeklySummary(
        DateOnly weekStart,
        DateOnly weekEnd,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        if (weekStart > weekEnd)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["weekStart"] = ["'weekStart' must be on or before 'weekEnd'."]
            });

        var userId = GetUserId(user);
        WeeklySummaryDto result = await dispatcher.SendAsync<GetWeeklySummaryQuery, WeeklySummaryDto>(
            new GetWeeklySummaryQuery(userId, weekStart, weekEnd), ct);
        return TypedResults.Ok(result);
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}
