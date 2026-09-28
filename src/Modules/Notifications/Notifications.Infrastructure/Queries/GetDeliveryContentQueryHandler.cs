namespace Notifications.Infrastructure.Queries;

using Dapper;
using Notifications.Application.Queries.GetDeliveryContent;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Sql;
using Shared.Abstractions.Cqrs;

internal sealed class GetDeliveryContentQueryHandler(DapperUnitOfWork uow)
    : IQueryHandler<GetDeliveryContentQuery, DeliveryContentDto?>
{
    public async Task<DeliveryContentDto?> HandleAsync(GetDeliveryContentQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var connection = await uow.GetConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<DeliveryContentDto>(new CommandDefinition(
            NotificationDeliverySql.SelectContent,
            new { Id = query.DeliveryId },
            cancellationToken: ct));
    }
}
