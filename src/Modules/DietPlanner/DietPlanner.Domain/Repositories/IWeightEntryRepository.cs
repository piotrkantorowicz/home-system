namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface IWeightEntryRepository
{
    Task<WeightEntry?> GetByIdAsync(WeightEntryId id, CancellationToken ct = default);
    Task<WeightEntry?> GetByUserAndDateAsync(string userId, DateOnly date, CancellationToken ct = default);
    Task<List<WeightEntry>> GetByUserAsync(string userId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<WeightEntry?> GetLatestByUserAsync(
        string userId, WeightEntryId? excludeId = null, CancellationToken ct = default);
    Task AddAsync(WeightEntry entry, CancellationToken ct = default);
    void Delete(WeightEntry entry);
}
