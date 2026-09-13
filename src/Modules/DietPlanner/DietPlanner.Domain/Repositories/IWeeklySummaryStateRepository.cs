namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;

public interface IWeeklySummaryStateRepository
{
    Task<WeeklySummaryState?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(WeeklySummaryState state, CancellationToken ct = default);
}
