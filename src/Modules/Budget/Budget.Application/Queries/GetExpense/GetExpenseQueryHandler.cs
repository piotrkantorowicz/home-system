namespace Budget.Application.Queries.GetExpense;

using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetExpenseQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetExpenseQuery, ExpenseDto>
{
    public async Task<ExpenseDto> HandleAsync(GetExpenseQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var id = ExpenseId.From(query.Id);

        var row = await db.VisibleTo(caller).Where(e => e.Id == id).Select(ExpenseReadExtensions.ToRow).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Expense", query.Id);

        return row.ToDto(caller);
    }
}
