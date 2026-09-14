namespace Notifications.Api;

using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

using Notifications.Application.Commands.UpdateChannelPreferences;
using Notifications.Application.Queries.GetChannelPreferences;
using Shared.Abstractions.Cqrs;

/// <summary>Endpoints for the caller's channel switches (<c>/api/notification-preferences</c>).</summary>
public static class NotificationChannelPreferencesEndpoints
{
    /// <summary>Maps the preferences read and update operations; both require an authenticated user.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapNotificationChannelPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notification-preferences")
            .RequireAuthorization()
            .WithTags("NotificationPreferences");

        group.MapGet("/", Get)
             .WithName("GetNotificationChannelPreferences")
             .Produces<ChannelPreferencesDto>(StatusCodes.Status200OK);

        group.MapPut("/", Update)
             .WithName("UpdateNotificationChannelPreferences")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem();

        return app;
    }

    private static async Task<Ok<ChannelPreferencesDto>> Get(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");

        var result = await dispatcher.SendAsync<GetChannelPreferencesQuery, ChannelPreferencesDto>(
            new GetChannelPreferencesQuery(userId), ct);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> Update(
        UpdateChannelPreferencesRequest request,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");

        await dispatcher.SendAsync(new UpdateChannelPreferencesCommand(
            userId,
            request.ConsoleEnabled,
            request.EmailEnabled,
            request.WebSocketEnabled), ct);

        return TypedResults.NoContent();
    }
}

/// <summary>
/// Body of the preferences update; all three switches are sent.
/// </summary>
/// <param name="ConsoleEnabled">Whether the console channel is on.</param>
/// <param name="EmailEnabled">Whether the email channel is on.</param>
/// <param name="WebSocketEnabled">Whether the WebSocket channel is on.</param>
public sealed record UpdateChannelPreferencesRequest(
    bool ConsoleEnabled,
    bool EmailEnabled,
    bool WebSocketEnabled);
