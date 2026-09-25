namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class WaterReminderStateRepository(DietPlannerDbContext dbContext)
    : IWaterReminderStateRepository
{
    public Task<WaterReminderState?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default)
        => dbContext.WaterReminderStates
            .FirstOrDefaultAsync(x => x.PersonId == personId, ct);

    public async Task AddAsync(WaterReminderState state, CancellationToken ct = default)
        => await dbContext.WaterReminderStates.AddAsync(state, ct);
}
