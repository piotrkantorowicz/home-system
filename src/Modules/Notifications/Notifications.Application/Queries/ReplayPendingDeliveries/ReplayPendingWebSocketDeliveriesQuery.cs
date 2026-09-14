namespace Notifications.Application.Queries.ReplayPendingDeliveries;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Lists the caller's WebSocket deliveries that were never acknowledged, so the hub can push them again on reconnect.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record ReplayPendingWebSocketDeliveriesQuery(string UserId)
    : IQuery<IReadOnlyList<ReplayPendingDeliveryDto>>;
