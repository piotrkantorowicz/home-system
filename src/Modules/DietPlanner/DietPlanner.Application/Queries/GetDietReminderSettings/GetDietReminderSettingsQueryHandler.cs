namespace DietPlanner.Application.Queries.GetDietReminderSettings;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetDietReminderSettingsQueryHandler : IQueryHandler<GetDietReminderSettingsQuery, DietReminderSettingsDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetDietReminderSettingsQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<DietReminderSettingsDto?> HandleAsync(GetDietReminderSettingsQuery query, CancellationToken ct = default)
        => await _dbContext.DietReminderSettings
            .AsNoTracking()
            .Where(s => s.UserId == query.UserId)
            .Select(s => new DietReminderSettingsDto(
                s.Id.Value,
                s.UserId,
                s.MealRemindersEnabled,
                s.MealReminderLeadTimeMinutes,
                s.MealMissedGraceMinutes,
                s.WaterRemindersEnabled,
                s.WaterReminderIntervalMinutes,
                s.WaterWindowStartUtc,
                s.WaterWindowEndUtc,
                s.WeeklySummaryEnabled,
                s.WeeklySummaryDayOfWeekUtc,
                s.WeeklySummaryTimeOfDayUtc,
                s.GoalAlertsEnabled,
                s.CreatedAt,
                s.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
