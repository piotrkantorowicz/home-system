namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class SentMealReminderRepository(DietPlannerDbContext dbContext) : ISentMealReminderRepository
{
    public Task<bool> ExistsAsync(MealEntryId mealEntryId, MealReminderKind kind, CancellationToken ct = default)
        => dbContext.SentMealReminders
            .AsNoTracking()
            .AnyAsync(x => x.MealEntryId == mealEntryId && x.Kind == kind, ct);

    public async Task AddAsync(SentMealReminder reminder, CancellationToken ct = default)
        => await dbContext.SentMealReminders.AddAsync(reminder, ct);
}
