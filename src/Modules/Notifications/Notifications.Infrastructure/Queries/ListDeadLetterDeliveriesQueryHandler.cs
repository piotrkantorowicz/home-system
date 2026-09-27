namespace Notifications.Infrastructure.Queries;

using Dapper;
using Microsoft.Extensions.Options;
using Notifications.Application.Queries.GetDeliveryBacklog;
using Notifications.Application.Queries.ListDeadLetterDeliveries;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Sql;
using Notifications.Infrastructure.Workers;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListDeadLetterDeliveriesQueryHandler(
    DapperUnitOfWork uow,
    IOptions<RetryDeliveryWorkerOptions> options)
    : IQueryHandler<ListDeadLetterDeliveriesQuery, PagedList<DeadLetterDeliveryDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedList<DeadLetterDeliveryDto>> HandleAsync(
        ListDeadLetterDeliveriesQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var maxAttempts = options.Value.MaxAttempts;
        var connection = await uow.GetConnectionAsync(ct);

        var items = await connection.QueryAsync<DeadLetterDeliveryDto>(new CommandDefinition(
            NotificationDeliverySql.ListDeadLetteredPaged,
            new { MaxAttempts = maxAttempts, Offset = (page - 1) * pageSize, Limit = pageSize },
            cancellationToken: ct));

        var backlog = await connection.QuerySingleAsync<DeliveryBacklogDto>(new CommandDefinition(
            NotificationDeliverySql.CountFailedByState,
            new { MaxAttempts = maxAttempts },
            cancellationToken: ct));

        return new PagedList<DeadLetterDeliveryDto>(items.ToList(), backlog.DeadLettered, page, pageSize);
    }
}
