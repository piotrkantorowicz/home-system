namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.DeleteWaterIntake;
using DietPlanner.Application.Commands.LogWaterIntake;
using DietPlanner.Application.Commands.UpdateHydrationConfig;
using DietPlanner.Application.Queries.GetHydrationConfig;
using DietPlanner.Application.Queries.GetWaterIntake;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Endpoints for hydration preferences and water logging (<c>/api/v1/hydration</c>).
/// </summary>
public static class HydrationEndpoints
{
    /// <summary>Maps the hydration config read/upsert and the water intake log/list/delete operations; all require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapHydrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/hydration")
            .WithTags("Hydration")
            .RequireAuthorization();

        group.MapGet("/config", GetHydrationConfig)
            .WithName("GetHydrationConfig")
            .WithSummary("Get the current user's hydration configuration")
            .WithDescription("Returns hydration settings including daily water target and glass size. Returns `null` body when no config has been set yet.");

        group.MapPut("/config", UpdateHydrationConfig)
            .WithName("UpdateHydrationConfig")
            .WithSummary("Create or update hydration configuration")
            .WithDescription("Sets daily water target, glass size, and tracking preference for the current user.");

        group.MapGet("/intake", GetWaterIntake)
            .WithName("GetWaterIntake")
            .WithSummary("Get water intake entries for a specific date")
            .WithDescription("Returns all water intake entries for the current user on the specified date, along with the total amount.");

        group.MapPost("/intake", LogWaterIntake)
            .WithName("LogWaterIntake")
            .WithSummary("Log a water intake entry")
            .WithDescription("Records a new water intake entry for the current user.");

        group.MapDelete("/intake/{id:guid}", DeleteWaterIntake)
            .WithName("DeleteWaterIntake")
            .WithSummary("Delete a water intake entry")
            .WithDescription("Removes a water intake entry belonging to the current user.");

        return app;
    }

    private static async Task<Ok<HydrationConfigDto>> GetHydrationConfig(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        HydrationConfigDto? result = await dispatcher.SendAsync<GetHydrationConfigQuery, HydrationConfigDto?>(
            new GetHydrationConfigQuery(personId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> UpdateHydrationConfig(
        UpdateHydrationConfigRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        await dispatcher.SendAsync(
            new UpdateHydrationConfigCommand(
                personId, request.DailyWaterTargetMl, request.GlassSizeMl, request.TrackWaterIntake), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<WaterIntakeListDto>> GetWaterIntake(
        DateOnly date,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        WaterIntakeListDto result = await dispatcher.SendAsync<GetWaterIntakeQuery, WaterIntakeListDto>(
            new GetWaterIntakeQuery(personId, date), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Created<Guid>> LogWaterIntake(
        LogWaterIntakeRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        var id = await dispatcher.SendAsync<LogWaterIntakeCommand, Guid>(
            new LogWaterIntakeCommand(personId, request.Date, request.AmountMl, request.Note), ct);
        return TypedResults.Created($"/api/v1/hydration/intake/{id}", id);
    }

    private static async Task<NoContent> DeleteWaterIntake(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        await dispatcher.SendAsync(new DeleteWaterIntakeCommand(id, personId), ct);
        return TypedResults.NoContent();
    }

    private static Guid GetPersonId(ClaimsPrincipal user)
        => PersonalDataClaims.GetPersonId(user);
}

/// <summary>
/// Body of the hydration config upsert.
/// </summary>
/// <param name="DailyWaterTargetMl">Daily target in millilitres; positive.</param>
/// <param name="GlassSizeMl">Volume one "glass" tap logs, in millilitres; positive.</param>
/// <param name="TrackWaterIntake">Whether water tracking and its reminders are enabled.</param>
public sealed record UpdateHydrationConfigRequest(
    int DailyWaterTargetMl,
    int GlassSizeMl,
    bool TrackWaterIntake);

/// <summary>
/// Body of the water intake log.
/// </summary>
/// <param name="Date">The calendar day the drink counts towards.</param>
/// <param name="AmountMl">Volume in millilitres; positive.</param>
/// <param name="Note">Optional free-text note.</param>
public sealed record LogWaterIntakeRequest(
    DateOnly Date,
    int AmountMl,
    string? Note);
