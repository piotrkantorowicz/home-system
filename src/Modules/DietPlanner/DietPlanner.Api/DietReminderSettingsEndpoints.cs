namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.UpdateDietReminderSettings;
using DietPlanner.Application.Queries.GetDietReminderSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

public static class DietReminderSettingsEndpoints
{
    public static IEndpointRouteBuilder MapDietReminderSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/diet-reminder-settings")
            .WithTags("DietReminderSettings")
            .RequireAuthorization();

        group.MapGet("/", GetDietReminderSettings)
            .WithName("GetDietReminderSettings")
            .WithSummary("Get the current user's diet reminder settings")
            .WithDescription("Returns diet reminder settings for the current user. Returns 404 when no settings have been configured yet.")
            .Produces<DietReminderSettingsDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/", UpdateDietReminderSettings)
            .WithName("UpdateDietReminderSettings")
            .WithSummary("Update the current user's diet reminder settings")
            .WithDescription("Creates or updates diet reminder settings for the current user. All times are UTC; the frontend converts from user-local time.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetDietReminderSettings(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        DietReminderSettingsDto? result = await dispatcher.SendAsync<GetDietReminderSettingsQuery, DietReminderSettingsDto?>(
            new GetDietReminderSettingsQuery(userId), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> UpdateDietReminderSettings(
        DietReminderSettingsRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateDietReminderSettingsCommand(
                userId,
                request.MealRemindersEnabled,
                request.MealReminderLeadTimeMinutes,
                request.MealMissedGraceMinutes,
                request.WaterRemindersEnabled,
                request.WaterReminderIntervalMinutes,
                request.WaterWindowStartUtc,
                request.WaterWindowEndUtc,
                request.WeeklySummaryEnabled,
                request.WeeklySummaryDayOfWeekUtc,
                request.WeeklySummaryTimeOfDayUtc,
                request.GoalAlertsEnabled), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record DietReminderSettingsRequest(
    bool MealRemindersEnabled,
    int MealReminderLeadTimeMinutes,
    int MealMissedGraceMinutes,
    bool WaterRemindersEnabled,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStartUtc,
    TimeOnly WaterWindowEndUtc,
    bool WeeklySummaryEnabled,
    DayOfWeek WeeklySummaryDayOfWeekUtc,
    TimeOnly WeeklySummaryTimeOfDayUtc,
    bool GoalAlertsEnabled);
