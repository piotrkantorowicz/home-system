namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateGoal;
using DietPlanner.Application.Commands.UpdateGoal;
using DietPlanner.Application.Queries.GetGoal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoints for the caller's nutrition goal (<c>/api/v1/goals</c>).
/// </summary>
public static class GoalEndpoints
{
    /// <summary>Maps goal create, read and update; all require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/goals")
            .WithTags("Goals")
            .RequireAuthorization();

        group.MapGet("/", GetGoal)
            .WithName("GetGoals")
            .WithSummary("Get the current user's nutrition goals")
            .WithDescription("Returns the active nutrition targets for the current user. Returns `null` body when no goals have been set yet.");

        group.MapPost("/", CreateGoal)
            .WithName("CreateGoals")
            .WithSummary("Create nutrition goals for the current user")
            .WithDescription("Sets daily nutrition targets for the current user. All fields are optional — omit any target you do not wish to track.");

        group.MapPut("/", UpdateGoal)
            .WithName("UpdateGoals")
            .WithSummary("Update nutrition goals for the current user")
            .WithDescription("Replaces all nutrition targets for the current user. Pass `null` for any field to clear that specific target.");

        return app;
    }

    private static async Task<Ok<GoalDto>> GetGoal(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        GoalDto? result = await dispatcher.SendAsync<GetGoalQuery, GoalDto?>(
            new GetGoalQuery(userId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Created> CreateGoal(
        GoalRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var id = await dispatcher.SendAsync<CreateGoalCommand, Guid>(
            new CreateGoalCommand(
                userId, request.DailyCalorieTarget, request.ProteinGrams,
                request.CarbsGrams, request.FatGrams, request.FiberGrams), ct);
        return TypedResults.Created($"/api/v1/goals/{id}");
    }

    private static async Task<NoContent> UpdateGoal(
        GoalRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateGoalCommand(
                userId, request.DailyCalorieTarget, request.ProteinGrams,
                request.CarbsGrams, request.FatGrams, request.FiberGrams), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

/// <summary>
/// Body of goal create and update; any target may be left unset.
/// </summary>
/// <param name="DailyCalorieTarget">Daily energy target in kcal, or <see langword="null"/>.</param>
/// <param name="ProteinGrams">Daily protein target in grams, or <see langword="null"/>.</param>
/// <param name="CarbsGrams">Daily carbohydrate target in grams, or <see langword="null"/>.</param>
/// <param name="FatGrams">Daily fat target in grams, or <see langword="null"/>.</param>
/// <param name="FiberGrams">Daily fibre target in grams, or <see langword="null"/>.</param>
public sealed record GoalRequest(
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams);
