namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;

public interface IWaterReminderStateRepository
{
    Task<WaterReminderState?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(WaterReminderState state, CancellationToken ct = default);
}
