namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="WaterIntake"/> entries. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IWaterIntakeRepository
{
    /// <summary>Loads a water intake entry by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked water intake entry, or <see langword="null"/> when it does not exist.</returns>
    Task<WaterIntake?> GetByIdAsync(WaterIntakeId id, CancellationToken ct = default);
    /// <summary>Loads every drink a user logged on one day, newest first.</summary>
    /// <param name="personId">Person identifier of the owner.</param>
    /// <param name="day">The calendar day.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<IReadOnlyList<WaterIntake>> GetByPersonAndDateAsync(Guid personId, DateOnly day, CancellationToken ct = default);
    /// <summary>Stages a new water intake entry; it is written when the unit of work commits.</summary>
    /// <param name="intake">The water intake entry to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(WaterIntake intake, CancellationToken ct = default);
    /// <summary>Marks a loaded water intake entry for removal; the write happens on commit.</summary>
    /// <param name="intake">The tracked water intake entry.</param>
    void Delete(WaterIntake intake);
}
