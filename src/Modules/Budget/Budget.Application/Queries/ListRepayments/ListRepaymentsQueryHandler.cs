namespace Budget.Application.Queries.ListRepayments;

using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListRepaymentsQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<ListRepaymentsQuery, PagedList<RepaymentDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedList<RepaymentDto>> HandleAsync(ListRepaymentsQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAdultAsync(query.AuthSubject, ct);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var repayments = db.Settlements.AsNoTracking()
            .Where(s => db.Budgets.Any(b => b.Id == s.BudgetId && b.HouseholdId == caller.HouseholdId));

        var total = await repayments.CountAsync(ct);
        var rows = await repayments
            .OrderByDescending(s => s.PaidOn).ThenByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        string Name(Guid id, string stored) => caller.Members.FirstOrDefault(m => m.PersonId == id)?.DisplayName ?? stored;

        return new PagedList<RepaymentDto>(
            [.. rows.Select(s => new RepaymentDto(
                s.Id.Value, s.FromPersonId, Name(s.FromPersonId, s.FromDisplayName), s.ToPersonId, Name(s.ToPersonId, s.ToDisplayName),
                s.Amount.ToString("F2", CultureInfo.InvariantCulture), s.PaidOn, s.Note, s.AddedByPersonId,
                Name(s.AddedByPersonId, s.AddedByDisplayName), s.Revision, s.IsVoided, s.VoidedAt, s.VoidReason, s.CreatedAt))],
            total, page, pageSize);
    }
}
