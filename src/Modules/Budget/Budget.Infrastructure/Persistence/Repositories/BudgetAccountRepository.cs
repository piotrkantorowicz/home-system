namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class BudgetAccountRepository(BudgetDbContext dbContext) : IBudgetAccountRepository
{
    public async Task AddAsync(BudgetAccount account, CancellationToken ct)
        => await dbContext.BudgetAccounts.AddAsync(account, ct);

    public async Task<BudgetAccount?> GetAsync(BudgetAccountId id, BudgetId budgetId, CancellationToken ct)
        => await dbContext.BudgetAccounts.FirstOrDefaultAsync(a => a.Id == id && a.BudgetId == budgetId, ct);
}
