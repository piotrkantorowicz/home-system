using DietPlanner.Api.Common.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Features.Goals;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/goals")
            .WithTags("Goals")
            .RequireAuthorization();

        group.MapGet("/", async (HttpContext context, [FromServices] IGoalService svc) =>
        {
            var userId = context.User.GetUserId();
            var result = await svc.GetAsync(userId);
            return result is not null ? Results.Ok(result) : Results.Ok(GoalResponse.Empty());
        })
        .RequireRateLimiting("api")
        .WithName("GetGoals")
        .WithSummary("Get current user's nutrition goals")
        .WithDescription("Returns the authenticated user's daily nutrition goals. Returns empty defaults if no goals have been set yet.")
        .Produces<GoalResponse>(StatusCodes.Status200OK);

        group.MapPut("/", async ([FromBody] UpsertGoalRequest req,
            HttpContext context,
            [FromServices] IGoalService svc,
            [FromServices] IValidator<UpsertGoalRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var result = await svc.UpsertAsync(userId, req);
            return Results.Ok(result);
        })
        .RequireRateLimiting("api")
        .WithName("UpsertGoals")
        .WithSummary("Create or update user's nutrition goals")
        .WithDescription("Upserts daily nutrition goals for the authenticated user. One goal record per user — creates on first call, updates on subsequent calls.")
        .Produces<GoalResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem();
    }
}
