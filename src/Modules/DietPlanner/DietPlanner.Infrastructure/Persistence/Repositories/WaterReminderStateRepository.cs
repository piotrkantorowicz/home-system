namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class WaterReminderStateRepository(DietPlannerDbContext dbContext)
    : IWaterReminderStateRepository
{
    public Task<WaterReminderState?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => dbContext.WaterReminderStates
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task AddAsync(WaterReminderState state, CancellationToken ct = default)
        => await dbContext.WaterReminderStates.AddAsync(state, ct);
}
