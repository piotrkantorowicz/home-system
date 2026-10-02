namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class BudgetRepository(BudgetDbContext dbContext) : IBudgetRepository
{
    public async Task LockHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        var key = householdId.ToString();
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
    }

    public async Task<BudgetAggregate?> GetByHouseholdAsync(Guid householdId, CancellationToken ct)
        => await dbContext.Budgets.FirstOrDefaultAsync(b => b.HouseholdId == householdId, ct);

    public async Task AddAsync(BudgetAggregate budget, CancellationToken ct)
        => await dbContext.Budgets.AddAsync(budget, ct);
}
