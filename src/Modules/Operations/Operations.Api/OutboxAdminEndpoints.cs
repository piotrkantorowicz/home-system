namespace Operations.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Operations.Application.Commands.RetryAllOutboxDeadLetters;
using Operations.Application.Commands.RetryOutboxDeadLetter;
using Operations.Application.Queries.GetOutboxBacklog;
using Operations.Application.Queries.GetOutboxPayload;
using Operations.Application.Queries.ListOutboxDeadLetters;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Web;

/// <summary>
/// Admin endpoints over every publishing module's outbox (<c>/api/admin/outbox</c>): backlog counts,
/// dead-lettered messages and their payloads, and retry of one or all of them. An unknown module or
/// message is a 404.
/// </summary>
internal static class OutboxAdminEndpoints
{
    /// <summary>Maps the outbox admin endpoints; all require <see cref="AdminAuthorization.PolicyName"/>.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapOutboxAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/outbox")
            .RequireAuthorization(AdminAuthorization.PolicyName)
            .WithTags("Admin");

        group.MapGet("/summary", GetSummary)
            .WithName("GetOutboxBacklog")
            .WithSummary("Dead-lettered and retrying outbox messages per module");
        group.MapGet("/{module}/dead-letters", ListDeadLetters)
            .WithName("ListOutboxDeadLetters");
        group.MapPost("/{module}/dead-letters/{id:guid}/retry", Retry)
            .WithName("RetryOutboxDeadLetter")
            .WithSummary("Retry a message as a new outbox row; the original is kept as history");
        group.MapGet("/{module}/dead-letters/{id:guid}/payload", GetPayload)
            .WithName("GetOutboxDeadLetterPayload")
            .WithSummary("The message's serialised event (may contain personal data; not cached)");
        group.MapPost("/{module}/dead-letters/retry-all", RetryAll)
            .WithName("RetryAllOutboxDeadLetters")
            .WithSummary("Retry every dead-lettered message of a module (up to 500 per call)");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<OutboxModuleBacklog>>> GetSummary(
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<GetOutboxBacklogQuery, IReadOnlyList<OutboxModuleBacklog>>(
            new GetOutboxBacklogQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<PagedList<OutboxDeadLetter>>> ListDeadLetters(
        string module,
        IQueryDispatcher dispatcher,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await dispatcher.SendAsync<ListOutboxDeadLettersQuery, PagedList<OutboxDeadLetter>>(
            new ListOutboxDeadLettersQuery(module, page, pageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> Retry(
        string module,
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        await dispatcher.SendAsync(new RetryOutboxDeadLetterCommand(module, id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<OutboxRetryAllResult>> RetryAll(
        string module,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<RetryAllOutboxDeadLettersCommand, OutboxRetryAllResult>(
            new RetryAllOutboxDeadLettersCommand(module), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<OutboxPayload>> GetPayload(
        string module,
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext http,
        CancellationToken ct)
    {
        var payload = await dispatcher.SendAsync<GetOutboxPayloadQuery, OutboxPayload>(
            new GetOutboxPayloadQuery(module, id), ct);

        // Payloads can carry personal data: keep them out of browser and proxy caches.
        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(payload);
    }
}
