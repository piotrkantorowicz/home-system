namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.PurgeUserData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Test-only endpoints (<c>/api/v1/test-support</c>), mapped only in Development or when <c>E2ETestSupport:Enabled</c> is set, so E2E workers can reset their own data.
/// </summary>
public static class TestSupportEndpoints
{
    /// <summary>Maps the purge-my-data operation; requires an authenticated user and only ever touches that user's rows.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
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
                "Only registered when the environment is Development or E2ETestSupport:Enabled is true.");

        return app;
    }

    private static async Task<NoContent> PurgeMyData(
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new PurgeUserDataCommand(userId, PersonalDataClaims.GetPersonId(user)), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}
