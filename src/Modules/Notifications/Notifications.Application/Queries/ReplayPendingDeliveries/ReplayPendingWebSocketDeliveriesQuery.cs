namespace Notifications.Application.Queries.ReplayPendingDeliveries;

using Shared.Abstractions.Cqrs;

public sealed record ReplayPendingWebSocketDeliveriesQuery(string UserId)
    : IQuery<IReadOnlyList<ReplayPendingDeliveryDto>>;
