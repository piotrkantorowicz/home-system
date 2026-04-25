namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.DeleteWaterIntake;
using DietPlanner.Application.Commands.LogWaterIntake;
using DietPlanner.Application.Commands.UpdateHydrationConfig;
using DietPlanner.Application.Queries.GetHydrationConfig;
using DietPlanner.Application.Queries.GetWaterIntake;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

public static class HydrationEndpoints
{
    public static IEndpointRouteBuilder MapHydrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/hydration")
            .WithTags("Hydration")
            .RequireAuthorization();

        group.MapGet("/config", GetHydrationConfig)
            .WithName("GetHydrationConfig")
            .WithSummary("Get the current user's hydration configuration")
            .WithDescription("Returns hydration settings including daily water target and glass size. Returns `null` body when no config has been set yet.")
            .Produces<HydrationConfigDto>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/config", UpdateHydrationConfig)
            .WithName("UpdateHydrationConfig")
            .WithSummary("Create or update hydration configuration")
            .WithDescription("Sets daily water target, glass size, and tracking preference for the current user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/intake", GetWaterIntake)
            .WithName("GetWaterIntake")
            .WithSummary("Get water intake entries for a specific date")
            .WithDescription("Returns all water intake entries for the current user on the specified date, along with the total amount.")
            .Produces<WaterIntakeListDto>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/intake", LogWaterIntake)
            .WithName("LogWaterIntake")
            .WithSummary("Log a water intake entry")
            .WithDescription("Records a new water intake entry for the current user.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/intake/{id:guid}", DeleteWaterIntake)
            .WithName("DeleteWaterIntake")
            .WithSummary("Delete a water intake entry")
            .WithDescription("Removes a water intake entry belonging to the current user.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetHydrationConfig(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        HydrationConfigDto? result = await dispatcher.SendAsync<GetHydrationConfigQuery, HydrationConfigDto?>(
            new GetHydrationConfigQuery(userId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> UpdateHydrationConfig(
        UpdateHydrationConfigRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateHydrationConfigCommand(
                userId, request.DailyWaterTargetMl, request.GlassSizeMl, request.TrackWaterIntake), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetWaterIntake(
        DateOnly date,
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        WaterIntakeListDto result = await dispatcher.SendAsync<GetWaterIntakeQuery, WaterIntakeListDto>(
            new GetWaterIntakeQuery(userId, date), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> LogWaterIntake(
        LogWaterIntakeRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var id = await dispatcher.SendAsync<LogWaterIntakeCommand, Guid>(
            new LogWaterIntakeCommand(userId, request.Date, request.AmountMl, request.Note), ct);
        return TypedResults.Created($"/api/v1/hydration/intake/{id}", id);
    }

    private static async Task<IResult> DeleteWaterIntake(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(new DeleteWaterIntakeCommand(id, userId), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record UpdateHydrationConfigRequest(
    int DailyWaterTargetMl,
    int GlassSizeMl,
    bool TrackWaterIntake);

public sealed record LogWaterIntakeRequest(
    DateOnly Date,
    int AmountMl,
    string? Note);
