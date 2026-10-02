namespace Budget.Application.Queries.ListAccounts;

using Budget.Application.Queries.GetAccount;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>Pages through the envelopes the caller may see, by name. Page size is capped at 100.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
/// <param name="IncludeArchived">Also return archived envelopes (needed to restore them).</param>
public sealed record ListAccountsQuery(string AuthSubject, int Page = 1, int PageSize = 50, bool IncludeArchived = false)
    : IQuery<PagedList<AccountDto>>;
