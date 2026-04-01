namespace DietPlanner.Application.Queries.GetNotificationPreferences;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetNotificationPreferencesQueryHandler : IQueryHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetNotificationPreferencesQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<NotificationPreferencesDto?> HandleAsync(GetNotificationPreferencesQuery query, CancellationToken ct = default)
        => await _dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(np => np.UserId == query.UserId)
            .Select(np => new NotificationPreferencesDto(
                np.Id.Value,
                np.UserId,
                np.MealReminderEnabled,
                np.MealReminderLeadTimeMinutes,
                np.WaterReminderEnabled,
                np.WaterReminderIntervalMinutes,
                np.WeeklySummaryEnabled,
                np.GoalMilestoneAlertsEnabled,
                np.CreatedAt,
                np.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
