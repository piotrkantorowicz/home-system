namespace Notifications.Application.Queries.GetDeliveryBacklog;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin view: counts failed deliveries, split into dead-lettered and still retrying.
/// </summary>
public sealed record GetDeliveryBacklogQuery : IQuery<DeliveryBacklogDto>;
