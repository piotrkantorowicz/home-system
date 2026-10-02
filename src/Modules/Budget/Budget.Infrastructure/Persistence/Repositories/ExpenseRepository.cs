namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.Entities;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class ExpenseRepository(BudgetDbContext dbContext) : IExpenseRepository
{
    public async Task AddAsync(Expense expense, CancellationToken ct)
        => await dbContext.Expenses.AddAsync(expense, ct);

    public async Task LockRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct)
    {
        var key = $"expense-request:{budgetId.Value}:{actorPersonId}:{clientRequestId}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
    }

    public async Task<ExpenseRevision?> FindRevisionByRequestAsync(
        BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct)
        => await dbContext.ExpenseRevisions.AsNoTracking().FirstOrDefaultAsync(
            r => r.BudgetId == budgetId && r.ActorPersonId == actorPersonId && r.ClientRequestId == clientRequestId, ct);
}
