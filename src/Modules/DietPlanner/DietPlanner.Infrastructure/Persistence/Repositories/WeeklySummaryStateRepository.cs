namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class WeeklySummaryStateRepository(DietPlannerDbContext dbContext)
    : IWeeklySummaryStateRepository
{
    public Task<WeeklySummaryState?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default)
        => dbContext.WeeklySummaryStates
            .FirstOrDefaultAsync(x => x.PersonId == personId, ct);

    public async Task AddAsync(WeeklySummaryState state, CancellationToken ct = default)
        => await dbContext.WeeklySummaryStates.AddAsync(state, ct);
}
