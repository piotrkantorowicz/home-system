namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface IMealEntryRepository
{
    Task<MealEntry?> GetByIdAsync(MealEntryId id, CancellationToken ct = default);
    Task<List<MealEntry>> GetByUserAndDateRangeAsync(string userId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<bool> AnyForSlotAsync(MealSlotId mealSlotId, CancellationToken ct = default);
    Task AddAsync(MealEntry entry, CancellationToken ct = default);
    void Update(MealEntry entry);
    void Delete(MealEntry entry);
}
