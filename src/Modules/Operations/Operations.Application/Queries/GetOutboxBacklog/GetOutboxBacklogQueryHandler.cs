namespace Operations.Application.Queries.GetOutboxBacklog;

using Microsoft.Extensions.Options;
using Operations.Application.Outbox;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class GetOutboxBacklogQueryHandler(
    OutboxModules modules,
    IOptions<OutboxWorkerOptions> options)
    : IQueryHandler<GetOutboxBacklogQuery, IReadOnlyList<OutboxModuleBacklog>>
{
    public async Task<IReadOnlyList<OutboxModuleBacklog>> HandleAsync(
        GetOutboxBacklogQuery query, CancellationToken ct = default)
    {
        var result = new List<OutboxModuleBacklog>();
        foreach (var module in modules.All)
        {
            var backlog = await modules.Store(module).CountAsync(options.Value.MaxAttempts, ct);
            result.Add(new OutboxModuleBacklog(module.Name, backlog.DeadLettered, backlog.Retrying));
        }

        return result;
    }
}
