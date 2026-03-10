using DietPlanner.Api.Common.Extensions;
using DietPlanner.Api.Features.DietPlans.Import;
using DietPlanner.Api.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Features.DietPlans;

public static class DietPlanEndpoints
{
    public static void MapDietPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/diet-plans")
            .WithTags("Diet Plans")
            .RequireAuthorization();

        // POST /api/v1/diet-plans - Create a new diet plan manually
        group.MapPost("/", async (
            HttpContext context,
            [FromBody] CreateDietPlanRequest request,
            [FromServices] IDietPlanService service,
            [FromServices] IValidator<CreateDietPlanRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = context.User.GetUserId();
            var result = await service.CreateAsync(userId, request);

            return Results.Created($"/api/v1/diet-plans/{result.Id}", result);
        })
        .RequireRateLimiting("api")
        .WithName("CreateDietPlan")
        .WithSummary("Create a new diet plan")
        .WithDescription("Creates an empty diet plan with a name and date range. Meals can be added manually afterwards.")
        .Produces<DietPlanDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/v1/diet-plans/validate - Validate import JSON (dry run)
        group.MapPost("/validate", async (
            HttpContext context,
            [FromBody] ImportDto importDto,
            [FromServices] IImportValidator validator) =>
        {
            var userId = context.User.GetUserId();
            var result = await validator.ValidateAsync(importDto, userId);

            return Results.Ok(result);
        })
        .RequireRateLimiting("import")
        .WithName("ValidateImport")
        .WithSummary("Validate import JSON (dry run)")
        .WithDescription("Validates the import JSON structure and checks for conflicts without executing the import. Returns detailed validation results.")
        .Produces<ValidationResultDto>(StatusCodes.Status200OK)
        .Produces<ValidationResultDto>(StatusCodes.Status400BadRequest);

        // POST /api/v1/diet-plans/import - Execute import
        group.MapPost("/import", async (
            HttpContext context,
            [FromBody] ImportDto importDto,
            [FromServices] IImportValidator validator,
            [FromServices] IImportExecutor executor) =>
        {
            var userId = context.User.GetUserId();

            // First validate
            var validationResult = await validator.ValidateAsync(importDto, userId);

            if (!validationResult.Valid)
            {
                return Results.BadRequest(new
                {
                    error = "Import validation failed",
                    validation = validationResult
                });
            }

            // If there are any errors, don't proceed
            if (validationResult.Summary.Errors > 0)
            {
                return Results.BadRequest(new
                {
                    error = "Cannot proceed with import due to validation errors",
                    validation = validationResult
                });
            }

            // Execute import
            var result = await executor.ExecuteAsync(importDto, userId);

            return Results.Created($"/api/v1/diet-plans/{result.DietPlanId}", result);
        })
        .RequireRateLimiting("import")
        .WithName("ExecuteImport")
        .WithSummary("Import diet plan from JSON")
        .WithDescription("Validates and imports a complete diet plan from JSON. Creates/updates products, recipes, and meal schedule.")
        .Produces<ImportResultDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/v1/diet-plans - List user's diet plans
        group.MapGet("/", async (
            HttpContext context,
            [FromServices] IDietPlanService service,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var userId = context.User.GetUserId();
            var result = await service.GetUserPlansAsync(userId, page, pageSize);

            return Results.Ok(result);
        })
        .RequireRateLimiting("api")
        .WithName("ListDietPlans")
        .WithSummary("List user's diet plans")
        .WithDescription("Returns paginated list of diet plans for the current user.")
        .Produces<PagedResult<DietPlanSummaryDto>>(StatusCodes.Status200OK);

        // GET /api/v1/diet-plans/{id} - Get diet plan details
        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext context,
            [FromServices] IDietPlanService service) =>
        {
            var userId = context.User.GetUserId();
            var plan = await service.GetByIdAsync(id, userId);

            return Results.Ok(plan);
        })
        .RequireRateLimiting("api")
        .WithName("GetDietPlan")
        .WithSummary("Get diet plan details")
        .WithDescription("Returns diet plan details with basic information.")
        .Produces<DietPlanDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/v1/diet-plans/{id} - Delete diet plan
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext context,
            [FromServices] IDietPlanService service,
            [FromQuery] bool permanent = false) =>
        {
            var userId = context.User.GetUserId();
            await service.DeleteAsync(id, userId, permanent);

            return Results.NoContent();
        })
        .RequireRateLimiting("api")
        .WithName("DeleteDietPlan")
        .WithSummary("Delete diet plan")
        .WithDescription("Deletes a diet plan and all associated meal entries. You can only delete plans you created. The 'permanent' flag is accepted for consistency but diet plans are always physically deleted.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/diet-plans/{id}/meals - Add a meal entry
        group.MapPost("/{id:guid}/meals", async (
            Guid id,
            HttpContext context,
            [FromBody] CreateMealEntryRequest request,
            [FromServices] IMealEntryService service,
            [FromServices] IValidator<CreateMealEntryRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = context.User.GetUserId();
            var result = await service.CreateAsync(id, userId, request);

            return Results.Created($"/api/v1/diet-plans/{id}/meals/{result.Id}", result);
        })
        .RequireRateLimiting("api")
        .WithName("CreateMealEntry")
        .WithSummary("Add a meal entry to a diet plan")
        .Produces<MealEntryDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/v1/diet-plans/{id}/meals/{mealId} - Update a meal entry
        group.MapPut("/{id:guid}/meals/{mealId:guid}", async (
            Guid id,
            Guid mealId,
            HttpContext context,
            [FromBody] UpdateMealEntryRequest request,
            [FromServices] IMealEntryService service,
            [FromServices] IValidator<UpdateMealEntryRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = context.User.GetUserId();
            var result = await service.UpdateAsync(id, mealId, userId, request);

            return Results.Ok(result);
        })
        .RequireRateLimiting("api")
        .WithName("UpdateMealEntry")
        .WithSummary("Update a meal entry")
        .Produces<MealEntryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/v1/diet-plans/{id}/meals/{mealId} - Delete a meal entry
        group.MapDelete("/{id:guid}/meals/{mealId:guid}", async (
            Guid id,
            Guid mealId,
            HttpContext context,
            [FromServices] IMealEntryService service) =>
        {
            var userId = context.User.GetUserId();
            await service.DeleteAsync(id, mealId, userId);

            return Results.NoContent();
        })
        .RequireRateLimiting("api")
        .WithName("DeleteMealEntry")
        .WithSummary("Delete a meal entry")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/v1/diet-plans/{id}/meals - Get meals for a date range
        group.MapGet("/{id:guid}/meals", async (
            Guid id,
            HttpContext context,
            [FromServices] IDietPlanService service,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to) =>
        {
            var userId = context.User.GetUserId();
            var meals = await service.GetMealsAsync(id, userId, from, to);

            return Results.Ok(meals);
        })
        .RequireRateLimiting("api")
        .WithName("GetMeals")
        .WithSummary("Get meals for a date range")
        .WithDescription("Returns all meals for a diet plan within the specified date range.")
        .Produces<List<MealEntryDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
