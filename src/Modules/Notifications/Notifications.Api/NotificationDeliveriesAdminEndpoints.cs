namespace Notifications.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Commands.RetryDelivery;
using Notifications.Application.Queries.GetDeliveryBacklog;
using Notifications.Application.Queries.ListDeadLetterDeliveries;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Admin endpoints over failed notification deliveries (<c>/api/admin/notifications/deliveries</c>):
/// backlog counts, the dead-letter list and a retry that hands a delivery back to the retry worker.
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
        group.MapPost("/{id:guid}/retry", Retry)
            .WithName("RetryNotificationDelivery")
            .WithSummary("Reset a failed delivery's attempts so the retry worker sends it again");

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

    private static async Task<NoContent> Retry(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        await dispatcher.SendAsync(new RetryDeliveryCommand(id), ct);
        return TypedResults.NoContent();
    }
}
