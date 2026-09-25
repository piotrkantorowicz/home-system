namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

/// <summary>
/// Write-side access to a user's <see cref="MealScheduleConfig"/>, loaded with its slots. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IMealScheduleConfigRepository
{
    /// <summary>Loads the user's meal schedule for mutation.</summary>
    /// <param name="personId">Person identifier of the owner.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked meal schedule, or <see langword="null"/> when the user has none yet.</returns>
    Task<MealScheduleConfig?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default);
    /// <summary>Stages a new meal schedule; it is written when the unit of work commits.</summary>
    /// <param name="config">The meal schedule to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(MealScheduleConfig config, CancellationToken ct = default);
    /// <summary>Marks a loaded meal schedule as modified; the write happens on commit.</summary>
    /// <param name="config">The tracked meal schedule.</param>
    void Update(MealScheduleConfig config);
}
