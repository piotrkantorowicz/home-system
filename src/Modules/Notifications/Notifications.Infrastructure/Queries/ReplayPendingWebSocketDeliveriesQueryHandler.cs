namespace Notifications.Infrastructure.Queries;

using Dapper;

using Notifications.Application.Queries.ReplayPendingDeliveries;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Sql;
using Shared.Abstractions.Cqrs;

internal sealed class ReplayPendingWebSocketDeliveriesQueryHandler(DapperUnitOfWork uow)
    : IQueryHandler<ReplayPendingWebSocketDeliveriesQuery, IReadOnlyList<ReplayPendingDeliveryDto>>
{
    public async Task<IReadOnlyList<ReplayPendingDeliveryDto>> HandleAsync(
        ReplayPendingWebSocketDeliveriesQuery query, CancellationToken ct = default)
    {
        var connection = await uow.GetConnectionAsync(ct);

        var rows = await connection.QueryAsync<ReplayPendingDeliveryDto>(new CommandDefinition(
            NotificationDeliverySql.SelectPendingByChannelForUser,
            new { query.UserId, Channel = NotificationChannel.WebSocket.ToString() },
            cancellationToken: ct));

        return rows.ToList();
    }
}
