namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Access to the <see cref="SentMealReminder"/> ledger the meal reminder job uses to send each notification kind once per entry.
/// </summary>
public interface ISentMealReminderRepository
{
    /// <summary>Whether the notification of this kind was already sent for the entry.</summary>
    /// <param name="mealEntryId">The meal entry.</param>
    /// <param name="kind">Which notification.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<bool> ExistsAsync(MealEntryId mealEntryId, MealReminderKind kind, CancellationToken ct = default);
    /// <summary>Stages a new ledger row; it is written when the unit of work commits.</summary>
    /// <param name="reminder">The ledger row to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(SentMealReminder reminder, CancellationToken ct = default);
}
