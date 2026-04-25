namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.UpdateMealSchedule;
using DietPlanner.Application.Queries.GetMealSchedule;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

public static class MealScheduleEndpoints
{
    public static IEndpointRouteBuilder MapMealScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/meal-schedule")
            .WithTags("MealSchedule")
            .RequireAuthorization();

        group.MapGet("/", GetMealSchedule)
            .WithName("GetMealSchedule")
            .WithSummary("Get the current user's meal schedule configuration")
            .WithDescription("Returns the configured meal slots for the current user. Returns `null` body when no schedule has been set yet.")
            .Produces<MealScheduleConfigDto>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/", UpdateMealSchedule)
            .WithName("UpdateMealSchedule")
            .WithSummary("Create or update meal schedule configuration")
            .WithDescription("Sets the meal slots for the current user. Replaces all existing slots.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetMealSchedule(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        MealScheduleConfigDto? result = await dispatcher.SendAsync<GetMealScheduleQuery, MealScheduleConfigDto?>(
            new GetMealScheduleQuery(userId), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> UpdateMealSchedule(
        UpdateMealScheduleRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var slots = request.Slots
            .Select(s => new MealSlotInput(s.Id, s.Name, s.DefaultTime))
            .ToList();

        await dispatcher.SendAsync(
            new UpdateMealScheduleCommand(userId, slots), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record MealSlotRequest(Guid? Id, string Name, string DefaultTime);
public sealed record UpdateMealScheduleRequest(IReadOnlyList<MealSlotRequest> Slots);
