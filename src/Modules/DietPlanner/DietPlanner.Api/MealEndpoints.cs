namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.BulkCompleteMealEntries;
using DietPlanner.Application.Commands.CompleteMealEntry;
using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Application.Commands.DeleteMealEntry;
using DietPlanner.Application.Commands.ExecuteImport;
using DietPlanner.Application.Commands.OverrideMealEntry;
using DietPlanner.Application.Commands.ResetMealEntry;
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

        group.MapPatch("/{id:guid}/complete", CompleteMealEntry)
            .WithName("CompleteMealEntry")
            .WithSummary("Mark a meal entry as done")
            .WithDescription("Confirms the user ate the meal as planned. Idempotent on already-Done entries; rejected with 422 if the entry has been Modified — reset the override first.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPatch("/{id:guid}/override", OverrideMealEntry)
            .WithName("OverrideMealEntry")
            .WithSummary("Record what was actually eaten instead of the planned meal")
            .WithDescription("Replaces the meal's actual recipe and/or ad-hoc product list. Override must include either a recipe or at least one product.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPatch("/{id:guid}/reset", ResetMealEntry)
            .WithName("ResetMealEntry")
            .WithSummary("Revert a meal entry to its planned state")
            .WithDescription("Clears Done/Modified status and any override data. Idempotent on already-Planned entries.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/bulk-complete", BulkCompleteMeals)
            .WithName("BulkCompleteMeals")
            .WithSummary("Mark all of the day's planned meals as done")
            .WithDescription("Marks every Planned entry on the supplied date as Done. Skips Done (idempotent) and Modified (intentional override). Returns the number of entries that transitioned.")
            .Produces<BulkCompleteMealsResponse>()
            .ProducesValidationProblem()
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

    private static async Task<IResult> CompleteMealEntry(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new CompleteMealEntryCommand(id, userId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> OverrideMealEntry(
        Guid id,
        OverrideMealEntryRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var products = request.ActualProducts
            .Select(p => new ActualProductInput(p.ProductId, p.Amount, p.Unit))
            .ToList();

        await dispatcher.SendAsync(
            new OverrideMealEntryCommand(id, userId, request.ActualRecipeId, products), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ResetMealEntry(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new ResetMealEntryCommand(id, userId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> BulkCompleteMeals(
        BulkCompleteMealsRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        BulkCompleteResult result = await dispatcher.SendAsync<BulkCompleteMealEntriesCommand, BulkCompleteResult>(
            new BulkCompleteMealEntriesCommand(userId, request.Date), ct);
        return TypedResults.Ok(new BulkCompleteMealsResponse(result.Completed));
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

public sealed record OverrideMealEntryRequest(
    Guid? ActualRecipeId,
    IReadOnlyList<ActualProductRequest> ActualProducts);

public sealed record ActualProductRequest(Guid ProductId, decimal Amount, string Unit);

public sealed record BulkCompleteMealsRequest(DateOnly Date);

public sealed record BulkCompleteMealsResponse(int Completed);
