namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.DeleteWeightEntry;
using DietPlanner.Application.Commands.LogWeightEntry;
using DietPlanner.Application.Queries.GetWeightEntries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoints for weigh-ins (<c>/api/v1/weight-entries</c>).
/// </summary>
public static class WeightEntryEndpoints
{
    /// <summary>Maps weight entry list, log and delete; all require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapWeightEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/weight-entries")
            .WithTags("WeightEntries")
            .RequireAuthorization();

        group.MapPost("/", LogWeightEntry)
            .WithName("LogWeightEntry")
            .WithSummary("Log a weight entry for a specific date")
            .WithDescription("Creates a new entry, or replaces the existing entry for the same date. Always updates the user's current weight.");

        group.MapGet("/", GetWeightEntries)
            .WithName("GetWeightEntries")
            .WithSummary("Get the current user's weight history")
            .WithDescription("Returns weight entries ordered by date ascending, optionally filtered by date range.");

        group.MapDelete("/{id:guid}", DeleteWeightEntry)
            .WithName("DeleteWeightEntry")
            .WithSummary("Delete a weight entry")
            .WithDescription("Deletes the entry and recomputes the user's current weight from the latest remaining entry (or null if none).");

        return app;
    }

    private static async Task<Created<LogWeightEntryResponse>> LogWeightEntry(
        LogWeightEntryRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        LogWeightEntryResult result = await dispatcher.SendAsync<LogWeightEntryCommand, LogWeightEntryResult>(
            new LogWeightEntryCommand(personId, request.Date, request.WeightKg), ct);
        return TypedResults.Created(
            $"/api/v1/weight-entries/{result.Id}",
            new LogWeightEntryResponse(result.Id, result.Created));
    }

    private static async Task<Results<Ok<IReadOnlyList<WeightEntryDto>>, ValidationProblem>> GetWeightEntries(
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

        var personId = GetPersonId(user);
        IReadOnlyList<WeightEntryDto> result = await dispatcher.SendAsync<GetWeightEntriesQuery, IReadOnlyList<WeightEntryDto>>(
            new GetWeightEntriesQuery(personId, from, to), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> DeleteWeightEntry(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        await dispatcher.SendAsync(new DeleteWeightEntryCommand(personId, id), ct);
        return TypedResults.NoContent();
    }

    private static Guid GetPersonId(ClaimsPrincipal user)
        => PersonalDataClaims.GetPersonId(user);
}

/// <summary>
/// Body of a weigh-in.
/// </summary>
/// <param name="Date">The day of the weigh-in; today or earlier.</param>
/// <param name="WeightKg">Weight in kilograms, 0.1–999.</param>
public sealed record LogWeightEntryRequest(DateOnly Date, decimal WeightKg);

/// <summary>
/// Result of a weigh-in.
/// </summary>
/// <param name="Id">Identifier of the entry that now holds the weight.</param>
/// <param name="Created">True when a new entry was created, false when the day's existing entry was corrected.</param>
public sealed record LogWeightEntryResponse(Guid Id, bool Created);
