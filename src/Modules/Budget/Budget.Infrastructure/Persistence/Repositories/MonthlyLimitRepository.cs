namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class MonthlyLimitRepository(BudgetDbContext dbContext) : IMonthlyLimitRepository
{
    public async Task LockAsync(BudgetAccountId accountId, BudgetMonth month, CancellationToken ct)
    {
        var key = $"limit:{accountId.Value}:{month}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
    }

    public async Task<MonthlyLimit?> GetAsync(BudgetAccountId accountId, BudgetMonth month, CancellationToken ct)
    {
        var start = month.Start;
        return await dbContext.MonthlyLimits.FirstOrDefaultAsync(l => l.BudgetAccountId == accountId && l.MonthStart == start, ct);
    }

    public async Task AddAsync(MonthlyLimit limit, CancellationToken ct)
        => await dbContext.MonthlyLimits.AddAsync(limit, ct);

    public void Remove(MonthlyLimit limit) => dbContext.MonthlyLimits.Remove(limit);
}
