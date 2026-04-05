namespace DietPlanner.Api;

using System.Security.Claims;
using DietPlanner.Application.Commands.UpdateNotificationPreferences;
using DietPlanner.Application.Queries.GetNotificationPreferences;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.CQRS;

public static class NotificationPreferencesEndpoints
{
    public static IEndpointRouteBuilder MapNotificationPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notification-preferences")
            .WithTags("NotificationPreferences")
            .RequireAuthorization();

        group.MapGet("/", GetNotificationPreferences)
            .WithName("GetNotificationPreferences")
            .WithSummary("Get the current user's notification preferences")
            .WithDescription("Returns notification preference settings for the current user. Returns 404 when no preferences have been configured yet.")
            .Produces<NotificationPreferencesDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/", UpdateNotificationPreferences)
            .WithName("UpdateNotificationPreferences")
            .WithSummary("Update the current user's notification preferences")
            .WithDescription("Creates or updates notification preference settings for the current user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetNotificationPreferences(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        NotificationPreferencesDto? result = await dispatcher.SendAsync<GetNotificationPreferencesQuery, NotificationPreferencesDto?>(
            new GetNotificationPreferencesQuery(userId), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> UpdateNotificationPreferences(
        NotificationPreferencesRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        await dispatcher.SendAsync(
            new UpdateNotificationPreferencesCommand(
                userId,
                request.MealReminderEnabled,
                request.MealReminderLeadTimeMinutes,
                request.WaterReminderEnabled,
                request.WaterReminderIntervalMinutes,
                request.WeeklySummaryEnabled,
                request.GoalMilestoneAlertsEnabled), ct);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("User ID not found in token");
}

public sealed record NotificationPreferencesRequest(
    bool MealReminderEnabled,
    int MealReminderLeadTimeMinutes,
    bool WaterReminderEnabled,
    int WaterReminderIntervalMinutes,
    bool WeeklySummaryEnabled,
    bool GoalMilestoneAlertsEnabled);
