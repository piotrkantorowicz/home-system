namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

public interface IHydrationConfigRepository
{
    Task<HydrationConfig?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(HydrationConfig config, CancellationToken ct = default);
    void Update(HydrationConfig config);
}
