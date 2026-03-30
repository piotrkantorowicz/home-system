namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateGoal;
using DietPlanner.Application.Commands.UpdateGoal;
using DietPlanner.Application.Queries.GetGoal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.CQRS;

public static class GoalEndpoints
{
    public static IEndpointRouteBuilder MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/goals")
            .WithTags("Goals")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/", GetGoal)
            .WithName("GetGoals")
            .WithSummary("Get the current user's nutrition goals")
            .Produces<GoalDto>();

        group.MapPost("/", CreateGoal)
            .WithName("CreateGoals")
            .WithSummary("Create nutrition goals for the current user")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/", UpdateGoal)
            .WithName("UpdateGoals")
            .WithSummary("Update nutrition goals for the current user")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetGoal(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        GoalDto? result = await dispatcher.SendAsync<GetGoalQuery, GoalDto?>(
            new GetGoalQuery(userId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateGoal(
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

    private static async Task<IResult> UpdateGoal(
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

public sealed record GoalRequest(
    int? DailyCalorieTarget,
    decimal? ProteinGrams,
    decimal? CarbsGrams,
    decimal? FatGrams,
    decimal? FiberGrams);
