namespace Investments.Application.Queries.ListSnapshots;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>Pages a visible wallet's balance history, newest first. Page size is capped at 100.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="WalletId">The wallet; not-found when missing or invisible.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record ListSnapshotsQuery(string AuthSubject, Guid WalletId, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<SnapshotDto>>;
