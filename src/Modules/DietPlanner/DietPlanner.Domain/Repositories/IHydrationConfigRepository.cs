namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

/// <summary>
/// Write-side access to a user's <see cref="HydrationConfig"/>. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IHydrationConfigRepository
{
    /// <summary>Loads the user's hydration config for mutation.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked hydration config, or <see langword="null"/> when the user has none yet.</returns>
    Task<HydrationConfig?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    /// <summary>Stages a new hydration config; it is written when the unit of work commits.</summary>
    /// <param name="config">The hydration config to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(HydrationConfig config, CancellationToken ct = default);
    /// <summary>Marks a loaded hydration config as modified; the write happens on commit.</summary>
    /// <param name="config">The tracked hydration config.</param>
    void Update(HydrationConfig config);
}
