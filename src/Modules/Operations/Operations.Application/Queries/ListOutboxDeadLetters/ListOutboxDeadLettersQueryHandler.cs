namespace Operations.Application.Queries.ListOutboxDeadLetters;

using Microsoft.Extensions.Options;
using Operations.Application.Outbox;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class ListOutboxDeadLettersQueryHandler(
    OutboxModules modules,
    IOptions<OutboxWorkerOptions> options)
    : IQueryHandler<ListOutboxDeadLettersQuery, PagedList<OutboxDeadLetter>>
{
    private const int MaxPageSize = 100;

    public Task<PagedList<OutboxDeadLetter>> HandleAsync(ListOutboxDeadLettersQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return modules.Store(query.Module).ListAsync(
            options.Value.MaxAttempts, Math.Max(query.Page, 1), Math.Clamp(query.PageSize, 1, MaxPageSize), ct);
    }
}
