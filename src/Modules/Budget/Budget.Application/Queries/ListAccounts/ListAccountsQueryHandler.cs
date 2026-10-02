namespace Budget.Application.Queries.ListAccounts;

using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Application.Queries.GetAccount;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListAccountsQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<ListAccountsQuery, PagedList<AccountDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedList<AccountDto>> HandleAsync(ListAccountsQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // Visibility is applied before the count, so hidden envelopes never leak through totals.
        var visible = db.BudgetAccounts.AsNoTracking()
            .Where(a => db.Budgets.Any(b => b.Id == a.BudgetId && b.HouseholdId == caller.HouseholdId))
            .VisibleTo(caller)
            .Where(a => query.IncludeArchived || !a.IsArchived);

        var total = await visible.CountAsync(ct);
        var items = await visible
            .OrderBy(a => a.Name).ThenBy(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(AccountVisibilityFilter.ToDto)
            .ToListAsync(ct);

        return new PagedList<AccountDto>(items, total, page, pageSize);
    }
}
