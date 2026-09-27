namespace Notifications.Application.Queries.ListDeadLetterDeliveries;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin view: pages through failed deliveries that used up their retry attempts, most recent failure first.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record ListDeadLetterDeliveriesQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedList<DeadLetterDeliveryDto>>;
