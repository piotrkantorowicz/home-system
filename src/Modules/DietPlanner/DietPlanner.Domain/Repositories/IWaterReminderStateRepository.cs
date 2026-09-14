namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;

/// <summary>
/// Access to the per-user <see cref="WaterReminderState"/> ledger the water reminder job reads and advances.
/// </summary>
public interface IWaterReminderStateRepository
{
    /// <summary>Loads the user's ledger row for update.</summary>
    /// <param name="userId">Auth subject of the user.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked row, or <see langword="null"/> before the first reminder.</returns>
    Task<WaterReminderState?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    /// <summary>Stages a new ledger row; it is written when the unit of work commits.</summary>
    /// <param name="state">The ledger row to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(WaterReminderState state, CancellationToken ct = default);
}
