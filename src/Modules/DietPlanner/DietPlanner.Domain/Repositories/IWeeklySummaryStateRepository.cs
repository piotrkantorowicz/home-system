using DietPlanner.Domain.Ledgers;

namespace DietPlanner.Domain.Repositories;

public interface IWeeklySummaryStateRepository
{
    Task<WeeklySummaryState?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(WeeklySummaryState state, CancellationToken ct = default);
}
