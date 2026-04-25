namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.DeleteWeightEntry;
using DietPlanner.Application.Commands.LogWeightEntry;
using DietPlanner.Application.Queries.GetWeightEntries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

public static class WeightEntryEndpoints
{
    public static IEndpointRouteBuilder MapWeightEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/weight-entries")
            .WithTags("WeightEntries")
            .RequireAuthorization();

        group.MapPost("/", LogWeightEntry)
            .WithName("LogWeightEntry")
            .WithSummary("Log a weight entry for a specific date")
            .WithDescription("Creates a new entry, or replaces the existing entry for the same date. Always updates the user's current weight.")
            .Produces<LogWeightEntryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/", GetWeightEntries)
            .WithName("GetWeightEntries")
            .WithSummary("Get the current user's weight history")
            .WithDescription("Returns weight entries ordered by date ascending, optionally filtered by date range.")
            .Produces<IReadOnlyList<WeightEntryDto>>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", DeleteWeightEntry)
            .WithName("DeleteWeightEntry")
            .WithSummary("Delete a weight entry")
            .WithDescription("Deletes the entry and recomputes the user's current weight from the latest remaining entry (or null if none).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> LogWeightEntry(
        LogWeightEntryRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        LogWeightEntryResult result = await dispatcher.SendAsync<LogWeightEntryCommand, LogWeightEntryResult>(
            new LogWeightEntryCommand(userId, request.Date, request.WeightKg), ct);
        return TypedResults.Created(
            $"/api/v1/weight-entries/{result.Id}",
            new LogWeightEntryResponse(result.Id, result.Created));
    }

    private static async Task<IResult> GetWeightEntries(
        DateOnly? from,
        DateOnly? to,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        if (from is not null && to is not null && from > to)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["from"] = ["'from' must be on or before 'to'."]
            });

        var userId = GetUserId(user);
        IReadOnlyList<WeightEntryDto> result = await dispatcher.SendAsync<GetWeightEntriesQuery, IReadOnlyList<WeightEntryDto>>(
            new GetWeightEntriesQuery(userId, from, to), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> DeleteWeightEntry(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new DeleteWeightEntryCommand(userId, id), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record LogWeightEntryRequest(DateOnly Date, decimal WeightKg);

public sealed record LogWeightEntryResponse(Guid Id, bool Created);
