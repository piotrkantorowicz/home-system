namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="MealEntry"/> aggregates, loaded with their actual-product lines. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IMealEntryRepository
{
    /// <summary>Loads a meal entry by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked meal entry, or <see langword="null"/> when it does not exist.</returns>
    Task<MealEntry?> GetByIdAsync(MealEntryId id, CancellationToken ct = default);
    /// <summary>Loads a user's entries within an inclusive date range, ordered by date then sequence.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="fromDate">First day to include, or <see langword="null"/> for no lower bound.</param>
    /// <param name="toDate">Last day to include, or <see langword="null"/> for no upper bound.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<List<MealEntry>> GetByUserAndDateRangeAsync(string userId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);
    /// <summary>Whether any entry references the slot — used to block removing a slot from the schedule.</summary>
    /// <param name="mealSlotId">The slot to check.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<bool> AnyForSlotAsync(MealSlotId mealSlotId, CancellationToken ct = default);
    /// <summary>Stages a new meal entry; it is written when the unit of work commits.</summary>
    /// <param name="entry">The meal entry to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(MealEntry entry, CancellationToken ct = default);
    /// <summary>Marks a loaded meal entry as modified; the write happens on commit.</summary>
    /// <param name="entry">The tracked meal entry.</param>
    void Update(MealEntry entry);
    /// <summary>Marks a loaded meal entry for removal; the write happens on commit.</summary>
    /// <param name="entry">The tracked meal entry.</param>
    void Delete(MealEntry entry);
}
