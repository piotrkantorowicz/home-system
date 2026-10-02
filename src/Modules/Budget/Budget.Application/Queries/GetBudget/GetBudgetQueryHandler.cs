namespace Budget.Application.Queries.GetBudget;

using Budget.Application.Common;
using Budget.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetBudgetQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetBudgetQuery, BudgetDto?>
{
    public async Task<BudgetDto?> HandleAsync(GetBudgetQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);

        var row = await db.Budgets.AsNoTracking()
            .Where(b => b.HouseholdId == caller.HouseholdId)
            .Select(b => new { b.Id, b.Currency })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new BudgetDto(row.Id.Value, row.Currency.ToString());
    }
}
