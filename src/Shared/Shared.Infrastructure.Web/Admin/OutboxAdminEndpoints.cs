namespace Shared.Infrastructure.Web.Admin;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Core.Pagination;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Admin endpoints over every publishing module's outbox (<c>/api/admin/outbox</c>): backlog counts,
/// dead-lettered messages and a retry that puts one back in the worker's queue.
/// </summary>
/// <remarks>
/// Messaging infrastructure rather than module behaviour, so these talk to the keyed
/// <see cref="IOutboxDeadLetterStore"/> directly instead of going through a module's dispatcher —
/// the store is the only thing each operation touches.
/// </remarks>
public static class OutboxAdminEndpoints
{
    private const int MaxPageSize = 100;

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
            .WithSummary("Reset a message's attempts so the outbox worker dispatches it again");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<OutboxModuleBacklog>>> GetSummary(
        IEnumerable<OutboxModule> modules,
        IServiceProvider services,
        IOptions<OutboxWorkerOptions> options,
        CancellationToken ct)
    {
        var result = new List<OutboxModuleBacklog>();
        foreach (var module in modules.OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            var backlog = await Store(services, module).CountAsync(options.Value.MaxAttempts, ct);
            result.Add(new OutboxModuleBacklog(module.Name, backlog.DeadLettered, backlog.Retrying));
        }

        return TypedResults.Ok<IReadOnlyList<OutboxModuleBacklog>>(result);
    }

    private static async Task<Results<Ok<PagedList<OutboxDeadLetter>>, NotFound>> ListDeadLetters(
        string module,
        IEnumerable<OutboxModule> modules,
        IServiceProvider services,
        IOptions<OutboxWorkerOptions> options,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20)
    {
        var match = Find(modules, module);
        if (match is null) return TypedResults.NotFound();

        var result = await Store(services, match).ListAsync(
            options.Value.MaxAttempts, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, NotFound>> Retry(
        string module,
        Guid id,
        IEnumerable<OutboxModule> modules,
        IServiceProvider services,
        CancellationToken ct)
    {
        var match = Find(modules, module);
        if (match is null) return TypedResults.NotFound();

        return await Store(services, match).RequeueAsync(id, ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static OutboxModule? Find(IEnumerable<OutboxModule> modules, string name)
        => modules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

    private static IOutboxDeadLetterStore Store(IServiceProvider services, OutboxModule module)
        => services.GetRequiredKeyedService<IOutboxDeadLetterStore>(module.Key);
}
