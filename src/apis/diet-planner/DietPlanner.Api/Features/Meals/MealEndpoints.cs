using DietPlanner.Api.Common.Extensions;
using DietPlanner.Api.Features.DietPlans.Import;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Features.Meals;

public static class MealEndpoints
{
    public static void MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/meals")
            .WithTags("Meals")
            .RequireAuthorization();

        // GET /api/v1/meals?from=YYYY-MM-DD&to=YYYY-MM-DD
        group.MapGet("/", async (
            HttpContext context,
            [FromServices] IMealService service,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to) =>
        {
            var userId = context.User.GetUserId();
            var meals = await service.GetAsync(userId, from, to);

            return Results.Ok(meals);
        })
        .RequireRateLimiting("api")
        .WithName("GetMeals")
        .WithSummary("Get meals for a date range")
        .WithDescription("Returns all meal entries for the current user within the specified date range.")
        .Produces<List<MealEntryDto>>(StatusCodes.Status200OK);

        // GET /api/v1/meals/nutrition-summary?from=YYYY-MM-DD&to=YYYY-MM-DD
        group.MapGet("/nutrition-summary", async (
            HttpContext context,
            [FromServices] IMealService service,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to) =>
        {
            var userId = context.User.GetUserId();
            var summary = await service.GetNutritionSummaryAsync(userId, from, to);

            return Results.Ok(summary);
        })
        .RequireRateLimiting("api")
        .WithName("GetNutritionSummary")
        .WithSummary("Get daily nutrition summary for a date range")
        .WithDescription("Returns aggregated daily nutrition totals from all meal entries for the current user.")
        .Produces<List<DailyNutritionDto>>(StatusCodes.Status200OK);

        // POST /api/v1/meals
        group.MapPost("/", async (
            HttpContext context,
            [FromBody] CreateMealEntryRequest request,
            [FromServices] IMealService service,
            [FromServices] IValidator<CreateMealEntryRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = context.User.GetUserId();
            var result = await service.CreateAsync(userId, request);

            return Results.Created($"/api/v1/meals/{result.Id}", result);
        })
        .RequireRateLimiting("api")
        .WithName("CreateMealEntry")
        .WithSummary("Add a meal entry")
        .Produces<MealEntryDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/v1/meals/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            HttpContext context,
            [FromBody] UpdateMealEntryRequest request,
            [FromServices] IMealService service,
            [FromServices] IValidator<UpdateMealEntryRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = context.User.GetUserId();
            var result = await service.UpdateAsync(id, userId, request);

            return Results.Ok(result);
        })
        .RequireRateLimiting("api")
        .WithName("UpdateMealEntry")
        .WithSummary("Update a meal entry")
        .Produces<MealEntryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/v1/meals/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext context,
            [FromServices] IMealService service) =>
        {
            var userId = context.User.GetUserId();
            await service.DeleteAsync(id, userId);

            return Results.NoContent();
        })
        .RequireRateLimiting("api")
        .WithName("DeleteMealEntry")
        .WithSummary("Delete a meal entry")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/meals/validate
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
        .WithDescription("Validates the import JSON structure and checks for conflicts without executing the import.")
        .Produces<ValidationResultDto>(StatusCodes.Status200OK);

        // POST /api/v1/meals/import
        group.MapPost("/import", async (
            HttpContext context,
            [FromBody] ImportDto importDto,
            [FromServices] IImportValidator validator,
            [FromServices] IImportExecutor executor) =>
        {
            var userId = context.User.GetUserId();

            var validationResult = await validator.ValidateAsync(importDto, userId);

            if (!validationResult.Valid || validationResult.Summary.Errors > 0)
            {
                return Results.BadRequest(new
                {
                    error = "Import validation failed",
                    validation = validationResult
                });
            }

            var result = await executor.ExecuteAsync(importDto, userId);

            return Results.Created("/api/v1/meals", result);
        })
        .RequireRateLimiting("import")
        .WithName("ExecuteImport")
        .WithSummary("Import meals from JSON")
        .WithDescription("Validates and imports meal entries from JSON. Creates/updates products, recipes, and meal schedule.")
        .Produces<ImportResultDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
