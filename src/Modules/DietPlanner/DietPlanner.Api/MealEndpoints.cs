namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Application.Commands.DeleteMealEntry;
using DietPlanner.Application.Commands.UpdateMealEntry;
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
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/", GetMeals)
            .WithName("GetMeals")
            .WithSummary("Get meal entries for a date range")
            .Produces<IReadOnlyList<MealEntryDto>>();

        group.MapGet("/nutrition-summary", GetNutritionSummary)
            .WithName("GetNutritionSummary")
            .WithSummary("Get daily nutrition summary for a date range")
            .Produces<IReadOnlyList<DailyNutritionDto>>();

        group.MapPost("/", CreateMealEntry)
            .WithName("CreateMealEntry")
            .WithSummary("Add a meal entry")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateMealEntry)
            .WithName("UpdateMealEntry")
            .WithSummary("Update a meal entry")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteMealEntry)
            .WithName("DeleteMealEntry")
            .WithSummary("Delete a meal entry")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

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
                userId, request.Date, request.MealType, request.RecipeId,
                request.Servings, request.Notes, request.MealTime, request.SequenceOrder), ct);
        return TypedResults.Created($"/api/v1/meals/{id}");
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
                id, userId, request.Date, request.MealType, request.RecipeId,
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
    string MealType,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder);

public sealed record UpdateMealEntryRequest(
    DateOnly Date,
    string MealType,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder);
