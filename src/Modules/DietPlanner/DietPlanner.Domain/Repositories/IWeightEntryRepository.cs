namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="WeightEntry"/> aggregates. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IWeightEntryRepository
{
    /// <summary>Loads a weight entry by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked weight entry, or <see langword="null"/> when it does not exist.</returns>
    Task<WeightEntry?> GetByIdAsync(WeightEntryId id, CancellationToken ct = default);
    /// <summary>Loads the user's entry for one day — there is at most one.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="day">The calendar day.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked entry, or <see langword="null"/> when nothing was logged that day.</returns>
    Task<WeightEntry?> GetByUserAndDateAsync(string userId, DateOnly day, CancellationToken ct = default);
    /// <summary>Loads a user's entries within an inclusive date range, oldest first.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="fromDate">First day to include, or <see langword="null"/> for no lower bound.</param>
    /// <param name="toDate">Last day to include, or <see langword="null"/> for no upper bound.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<List<WeightEntry>> GetByUserAsync(string userId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);
    /// <summary>Loads the user's most recent entry by date (then creation time), used to keep the profile's current weight in sync.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="excludeId">An entry to ignore — the one being deleted or changed.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The latest remaining entry, or <see langword="null"/> when there is none.</returns>
    Task<WeightEntry?> GetLatestByUserAsync(
        string userId, WeightEntryId? excludeId = null, CancellationToken ct = default);
    /// <summary>Stages a new weight entry; it is written when the unit of work commits.</summary>
    /// <param name="entry">The weight entry to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(WeightEntry entry, CancellationToken ct = default);
    /// <summary>Marks a loaded weight entry for removal; the write happens on commit.</summary>
    /// <param name="entry">The tracked weight entry.</param>
    void Delete(WeightEntry entry);
}
