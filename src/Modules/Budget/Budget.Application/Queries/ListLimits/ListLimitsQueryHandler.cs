namespace Budget.Application.Queries.ListLimits;

using System.Globalization;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class ListLimitsQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<ListLimitsQuery, IReadOnlyList<MonthlyLimitDto>>
{
    public async Task<IReadOnlyList<MonthlyLimitDto>> HandleAsync(ListLimitsQuery query, CancellationToken ct = default)
    {
        if (!BudgetMonth.TryParse(query.Month, out var month))
            throw new CommandValidationException(nameof(ListLimitsQuery), [new ValidationError(nameof(query.Month), "Month must be YYYY-MM.")]);

        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var start = month.Start;
        var visibleIds = db.BudgetAccounts
            .Where(a => db.Budgets.Any(b => b.Id == a.BudgetId && b.HouseholdId == caller.HouseholdId))
            .VisibleTo(caller)
            .Select(a => a.Id);

        var rows = await db.MonthlyLimits.AsNoTracking()
            .Where(l => l.MonthStart == start && visibleIds.Contains(l.BudgetAccountId))
            .OrderBy(l => l.BudgetAccountId)
            .Select(l => new { l.BudgetAccountId, l.Amount, l.Revision })
            .ToListAsync(ct);

        return [.. rows.Select(r => new MonthlyLimitDto(
            r.BudgetAccountId.Value, month.ToString(), r.Amount.ToString("F2", CultureInfo.InvariantCulture), r.Revision))];
    }
}
