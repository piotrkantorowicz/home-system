namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.UpdateMealSchedule;
using DietPlanner.Application.Queries.GetMealSchedule;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoints for the caller's meal schedule (<c>/api/v1/meal-schedule</c>).
/// </summary>
public static class MealScheduleEndpoints
{
    /// <summary>Maps the schedule read and replace operations; all require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapMealScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/meal-schedule")
            .WithTags("MealSchedule")
            .RequireAuthorization();

        group.MapGet("/", GetMealSchedule)
            .WithName("GetMealSchedule")
            .WithSummary("Get the current user's meal schedule configuration")
            .WithDescription("Returns the configured meal slots for the current user. Returns `null` body when no schedule has been set yet.");

        group.MapPut("/", UpdateMealSchedule)
            .WithName("UpdateMealSchedule")
            .WithSummary("Create or update meal schedule configuration")
            .WithDescription("Sets the meal slots for the current user. Replaces all existing slots.");

        return app;
    }

    private static async Task<Ok<MealScheduleConfigDto>> GetMealSchedule(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        MealScheduleConfigDto? result = await dispatcher.SendAsync<GetMealScheduleQuery, MealScheduleConfigDto?>(
            new GetMealScheduleQuery(personId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> UpdateMealSchedule(
        UpdateMealScheduleRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        var slots = request.Slots
            .Select(s => new MealSlotInput(s.Id, s.Name, s.DefaultTime))
            .ToList();

        await dispatcher.SendAsync(
            new UpdateMealScheduleCommand(personId, slots), ct);
        return TypedResults.NoContent();
    }

    private static Guid GetPersonId(ClaimsPrincipal user)
        => PersonalDataClaims.GetPersonId(user);
}

/// <summary>
/// One desired slot in a schedule replace.
/// </summary>
/// <param name="Id">Identifier of an existing slot to keep, or <see langword="null"/> for a new slot.</param>
/// <param name="Name">Display name; required.</param>
/// <param name="DefaultTime">Default time of day as <c>HH:mm</c>.</param>
public sealed record MealSlotRequest(Guid? Id, string Name, string DefaultTime);
/// <summary>
/// Body of the schedule replace; slots omitted from the list are removed.
/// </summary>
/// <param name="Slots">The desired slots in display order; 1 to 8.</param>
public sealed record UpdateMealScheduleRequest(IReadOnlyList<MealSlotRequest> Slots);
