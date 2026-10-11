namespace Investments.Application.Queries.ListWallets;

using Investments.Application.Queries.GetWallet;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>Pages through the wallets the caller may see, by kind then name. Page size is capped at 100.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
/// <param name="IncludeArchived">Also return archived wallets (needed to restore them).</param>
public sealed record ListWalletsQuery(string AuthSubject, int Page = 1, int PageSize = 50, bool IncludeArchived = false)
    : IQuery<PagedList<WalletDto>>;
