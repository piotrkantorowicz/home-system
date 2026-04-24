namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Application.Commands.DeleteMealEntry;
using DietPlanner.Application.Commands.ExecuteImport;
using DietPlanner.Application.Commands.UpdateMealEntry;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.Application.Queries.GetNutritionSummary;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.CQRS;

public static class MealEndpoints
{
    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/meals")
            .WithTags("Meals")
            .RequireAuthorization();

        group.MapGet("/", GetMeals)
            .WithName("GetMeals")
            .WithSummary("Get meal entries for a date range")
            .WithDescription("Returns all meal entries logged by the current user within the specified date range. Omit `from`/`to` to return all entries.")
            .Produces<IReadOnlyList<MealEntryDto>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/nutrition-summary", GetNutritionSummary)
            .WithName("GetNutritionSummary")
            .WithSummary("Get daily nutrition summary for a date range")
            .WithDescription("Aggregates meal entries by day and returns total calories, protein, carbohydrates, fat, and fibre for each day in the range.")
            .Produces<IReadOnlyList<DailyNutritionDto>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", CreateMealEntry)
            .WithName("CreateMealEntry")
            .WithSummary("Add a meal entry")
            .WithDescription("Records a recipe serving in the current user's meal log. `mealType` identifies the meal slot (e.g. Breakfast, Lunch, Dinner, Snack). `servings` is a multiplier applied to the recipe's nutritional values.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", UpdateMealEntry)
            .WithName("UpdateMealEntry")
            .WithSummary("Update a meal entry")
            .WithDescription("Updates the date, meal type, servings, and notes of an existing meal entry. Only the owner of the entry may update it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", DeleteMealEntry)
            .WithName("DeleteMealEntry")
            .WithSummary("Delete a meal entry")
            .WithDescription("Permanently removes a meal entry from the log. Only the entry owner may delete it.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/validate", ValidateImport)
            .WithName("ValidateImport")
            .WithSummary("Validate import JSON (dry run)")
            .WithDescription("Validates the import JSON structure and checks for conflicts without executing the import.")
            .Produces<ValidationResultDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/import", ExecuteImport)
            .WithName("ExecuteImport")
            .WithSummary("Import meals from JSON")
            .WithDescription("Validates and imports meal entries from JSON. Creates/updates products, recipes, and meal schedule.")
            .Produces<ImportResultDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetMeals(
        [AsParameters] MealDateRangeParams @params,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        IReadOnlyList<MealEntryDto> result = await dispatcher.SendAsync<GetMealEntriesQuery, IReadOnlyList<MealEntryDto>>(
            new GetMealEntriesQuery(userId, @params.From, @params.To), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetNutritionSummary(
        [AsParameters] MealDateRangeParams @params,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        IReadOnlyList<DailyNutritionDto> result = await dispatcher.SendAsync<GetNutritionSummaryQuery, IReadOnlyList<DailyNutritionDto>>(
            new GetNutritionSummaryQuery(userId, @params.From, @params.To), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateMealEntry(
        CreateMealEntryRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var id = await dispatcher.SendAsync<CreateMealEntryCommand, Guid>(
            new CreateMealEntryCommand(
                userId, request.Date, request.MealSlotId, request.RecipeId,
                request.Servings, request.Notes, request.MealTime, request.SequenceOrder), ct);
        return TypedResults.Created($"/api/v1/meals/{id}", id);
    }

    private static async Task<IResult> UpdateMealEntry(
        Guid id,
        UpdateMealEntryRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateMealEntryCommand(
                id, userId, request.Date, request.MealSlotId, request.RecipeId,
                request.Servings, request.Notes, request.MealTime, request.SequenceOrder), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteMealEntry(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new DeleteMealEntryCommand(id, userId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ValidateImport(
        ImportDto request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        ValidationResultDto result = await dispatcher.SendAsync<ValidateImportCommand, ValidationResultDto>(
            new ValidateImportCommand(request, userId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ExecuteImport(
        ImportDto request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        ImportResultDto result = await dispatcher.SendAsync<ExecuteImportCommand, ImportResultDto>(
            new ExecuteImportCommand(request, userId), ct);
        return TypedResults.Created("/api/v1/meals", result);
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record MealDateRangeParams(
    [property: FromQuery] DateOnly? From,
    [property: FromQuery] DateOnly? To);

public sealed record CreateMealEntryRequest(
    DateOnly Date,
    Guid MealSlotId,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder);

public sealed record UpdateMealEntryRequest(
    DateOnly Date,
    Guid MealSlotId,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder);
