namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

/// <summary>
/// Write-side access to a user's <see cref="UserGoal"/>. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IUserGoalRepository
{
    /// <summary>Loads the user's goal for mutation.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked goal, or <see langword="null"/> when the user has none yet.</returns>
    Task<UserGoal?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    /// <summary>Stages a new goal; it is written when the unit of work commits.</summary>
    /// <param name="goal">The goal to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(UserGoal goal, CancellationToken ct = default);
    /// <summary>Marks a loaded goal as modified; the write happens on commit.</summary>
    /// <param name="goal">The tracked goal.</param>
    void Update(UserGoal goal);
}
