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

        group.MapPost("/", async ([FromBody] CreateGoalRequest req,
            HttpContext context,
            [FromServices] IGoalService svc,
            [FromServices] IValidator<CreateGoalRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var result = await svc.CreateAsync(userId, req);
            return Results.Created("/api/v1/goals", result);
        })
        .RequireRateLimiting("api")
        .WithName("CreateGoals")
        .WithSummary("Create user's nutrition goals")
        .WithDescription("Creates daily nutrition goals for the authenticated user. Fails if goals already exist — use PUT to update.")
        .Produces<GoalResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapPut("/", async ([FromBody] UpdateGoalRequest req,
            HttpContext context,
            [FromServices] IGoalService svc,
            [FromServices] IValidator<UpdateGoalRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var userId = context.User.GetUserId();
            var result = await svc.UpdateAsync(userId, req);
            return Results.Ok(result);
        })
        .RequireRateLimiting("api")
        .WithName("UpdateGoals")
        .WithSummary("Update user's nutrition goals")
        .WithDescription("Updates existing daily nutrition goals for the authenticated user. Fails if no goals exist — use POST to create first.")
        .Produces<GoalResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);
    }
}
