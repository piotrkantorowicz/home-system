namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.PurgeUserData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.CQRS;

public static class TestSupportEndpoints
{
    public static IEndpointRouteBuilder MapTestSupportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/test-support")
            .WithTags("TestSupport")
            .RequireAuthorization();

        group.MapDelete("/purge-my-data", PurgeMyData)
            .WithName("PurgeMyData")
            .WithSummary("Hard-delete all data owned by the current user")
            .WithDescription(
                "Test-only endpoint. Removes every row owned by the authenticated user across all DietPlanner aggregates " +
                "(meals, recipes, products, goals, profile, hydration, notifications, schedules). " +
                "Only registered when the environment is Development or E2ETestSupport:Enabled is true.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> PurgeMyData(
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new PurgeUserDataCommand(userId), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}
