using DietPlanner.Domain.Ledgers;

namespace DietPlanner.Domain.Repositories;

public interface IWaterReminderStateRepository
{
    Task<WaterReminderState?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(WaterReminderState state, CancellationToken ct = default);
}
