namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;

public interface ISentMealReminderRepository
{
    Task<bool> ExistsAsync(MealEntryId mealEntryId, MealReminderKind kind, CancellationToken ct = default);
    Task AddAsync(SentMealReminder reminder, CancellationToken ct = default);
}
