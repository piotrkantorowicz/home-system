namespace Notifications.Api;

using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Notifications.Application.Commands.MarkNotificationRead;
using Notifications.Application.Queries.ListNotifications;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        group.MapGet("/", ListNotifications)
             .WithName("ListNotifications")
             .Produces<PagedList<NotificationDto>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/read", MarkRead)
             .WithName("MarkNotificationRead")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<PagedList<NotificationDto>>> ListNotifications(
        ClaimsPrincipal user,
        IQueryDispatcher dispatcher,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");

        var result = await dispatcher.SendAsync<ListNotificationsQuery, PagedList<NotificationDto>>(
            new ListNotificationsQuery(userId, page, pageSize), ct);

        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> MarkRead(
        Guid id,
        ClaimsPrincipal user,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");

        await dispatcher.SendAsync(new MarkNotificationReadCommand(id, userId), ct);
        return TypedResults.NoContent();
    }
}
