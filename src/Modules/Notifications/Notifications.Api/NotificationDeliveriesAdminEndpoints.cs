namespace Notifications.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Commands.RetryAllDeliveries;
using Notifications.Application.Commands.RetryDelivery;
using Notifications.Application.Queries.GetDeliveryBacklog;
using Notifications.Application.Queries.GetDeliveryContent;
using Notifications.Application.Queries.ListDeadLetterDeliveries;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Admin endpoints over failed notification deliveries (<c>/api/admin/notifications/deliveries</c>):
/// backlog counts, the dead-letter list, a delivery's content, and retry of one or all dead letters.
/// </summary>
public static class NotificationDeliveriesAdminEndpoints
{
    /// <summary>Maps the delivery admin endpoints; all require <see cref="AdminAuthorization.PolicyName"/>.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapNotificationDeliveriesAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/notifications/deliveries")
            .RequireAuthorization(AdminAuthorization.PolicyName)
            .WithTags("Admin");

        group.MapGet("/summary", GetBacklog)
            .WithName("GetNotificationDeliveryBacklog")
            .WithSummary("Dead-lettered and retrying notification deliveries");
        group.MapGet("/dead-letters", ListDeadLetters)
            .WithName("ListDeadLetterDeliveries");
        group.MapGet("/{id:guid}/content", GetContent)
            .WithName("GetNotificationDeliveryContent")
            .WithSummary("The notification a delivery carries (may contain personal data; not cached)");
        group.MapPost("/{id:guid}/retry", Retry)
            .WithName("RetryNotificationDelivery")
            .WithSummary("Retry a failed delivery as a new one; the original is kept as history");
        group.MapPost("/retry-all", RetryAll)
            .WithName("RetryAllNotificationDeliveries")
            .WithSummary("Retry every dead-lettered delivery (up to 500 per call)");

        return app;
    }

    private static async Task<Ok<DeliveryBacklogDto>> GetBacklog(
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<GetDeliveryBacklogQuery, DeliveryBacklogDto>(
            new GetDeliveryBacklogQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<PagedList<DeadLetterDeliveryDto>>> ListDeadLetters(
        IQueryDispatcher dispatcher,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await dispatcher.SendAsync<ListDeadLetterDeliveriesQuery, PagedList<DeadLetterDeliveryDto>>(
            new ListDeadLetterDeliveriesQuery(page, pageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<DeliveryContentDto>, NotFound>> GetContent(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<GetDeliveryContentQuery, DeliveryContentDto?>(
            new GetDeliveryContentQuery(id), ct);
        if (result is null) return TypedResults.NotFound();

        // Content can carry personal data: keep it out of browser and proxy caches.
        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> Retry(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        await dispatcher.SendAsync(new RetryDeliveryCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<RetryAllDeliveriesResultDto>> RetryAll(
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<RetryAllDeliveriesCommand, RetryAllDeliveriesResultDto>(
            new RetryAllDeliveriesCommand(), ct);
        return TypedResults.Ok(result);
    }
}
