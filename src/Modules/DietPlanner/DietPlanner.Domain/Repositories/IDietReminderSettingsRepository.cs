namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

/// <summary>
/// Write-side access to a user's <see cref="DietReminderSettings"/>. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IDietReminderSettingsRepository
{
    /// <summary>Loads the user's reminder settings for mutation.</summary>
    /// <param name="userId">Auth subject of the owner.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked reminder settings, or <see langword="null"/> when the user has none yet.</returns>
    Task<DietReminderSettings?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    /// <summary>Stages a new reminder settings; it is written when the unit of work commits.</summary>
    /// <param name="settings">The reminder settings to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(DietReminderSettings settings, CancellationToken ct = default);
    /// <summary>Marks a loaded reminder settings as modified; the write happens on commit.</summary>
    /// <param name="settings">The tracked reminder settings.</param>
    void Update(DietReminderSettings settings);
}
