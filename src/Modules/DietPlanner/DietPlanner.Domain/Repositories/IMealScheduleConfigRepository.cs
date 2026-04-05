namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

public interface IMealScheduleConfigRepository
{
    Task<MealScheduleConfig?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(MealScheduleConfig config, CancellationToken ct = default);
    void Update(MealScheduleConfig config);
}
