namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

/// <summary>
/// Write-side access to a user's <see cref="UserProfile"/>. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>Loads the user's profile for mutation.</summary>
    /// <param name="personId">Person identifier of the owner.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked profile, or <see langword="null"/> when the user has none yet.</returns>
    Task<UserProfile?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default);
    /// <summary>Stages a new profile; it is written when the unit of work commits.</summary>
    /// <param name="profile">The profile to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(UserProfile profile, CancellationToken ct = default);
    /// <summary>Marks a loaded profile as modified; the write happens on commit.</summary>
    /// <param name="profile">The tracked profile.</param>
    void Update(UserProfile profile);
}
