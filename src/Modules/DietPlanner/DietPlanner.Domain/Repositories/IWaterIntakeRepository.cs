namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface IWaterIntakeRepository
{
    Task<WaterIntake?> GetByIdAsync(WaterIntakeId id, CancellationToken ct = default);
    Task<IReadOnlyList<WaterIntake>> GetByUserAndDateAsync(string userId, DateOnly day, CancellationToken ct = default);
    Task AddAsync(WaterIntake intake, CancellationToken ct = default);
    void Delete(WaterIntake intake);
}
