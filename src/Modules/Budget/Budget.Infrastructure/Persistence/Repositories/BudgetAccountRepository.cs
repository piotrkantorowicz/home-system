namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;

internal sealed class BudgetAccountRepository(BudgetDbContext dbContext) : IBudgetAccountRepository
{
    public async Task AddAsync(BudgetAccount account, CancellationToken ct)
        => await dbContext.BudgetAccounts.AddAsync(account, ct);
}
