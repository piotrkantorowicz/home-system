namespace Budget.Application.Queries.ListRepayments;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

/// <summary>Pages through recorded repayments, newest payment date first (voided ones included, marked). Owner/Adult only; page size capped at 100.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page.</param>
public sealed record ListRepaymentsQuery(string AuthSubject, int Page = 1, int PageSize = 20) : IQuery<PagedList<RepaymentDto>>;
