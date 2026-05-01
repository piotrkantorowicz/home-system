namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class WeeklySummaryStateRepository(DietPlannerDbContext dbContext)
    : IWeeklySummaryStateRepository
{
    public Task<WeeklySummaryState?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => dbContext.WeeklySummaryStates
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task AddAsync(WeeklySummaryState state, CancellationToken ct = default)
        => await dbContext.WeeklySummaryStates.AddAsync(state, ct);
}
