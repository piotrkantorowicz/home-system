namespace Notifications.Infrastructure.Queries;

using Dapper;
using Microsoft.Extensions.Options;
using Notifications.Application.Queries.GetDeliveryBacklog;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Sql;
using Notifications.Infrastructure.Workers;
using Shared.Abstractions.Cqrs;

internal sealed class GetDeliveryBacklogQueryHandler(
    DapperUnitOfWork uow,
    IOptions<RetryDeliveryWorkerOptions> options)
    : IQueryHandler<GetDeliveryBacklogQuery, DeliveryBacklogDto>
{
    public async Task<DeliveryBacklogDto> HandleAsync(
        GetDeliveryBacklogQuery query, CancellationToken ct = default)
    {
        var connection = await uow.GetConnectionAsync(ct);
        return await connection.QuerySingleAsync<DeliveryBacklogDto>(new CommandDefinition(
            NotificationDeliverySql.CountFailedByState,
            new { options.Value.MaxAttempts },
            cancellationToken: ct));
    }
}
