namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class DietReminderSettings : AggregateRoot<DietReminderSettingsId>
{
    private DietReminderSettings() { }

    public static DietReminderSettings Create(
        DietReminderSettingsId id,
        string userId,
        bool mealRemindersEnabled = true,
        int mealReminderLeadTimeMinutes = 15,
        int mealMissedGraceMinutes = 30,
        bool waterRemindersEnabled = true,
        int waterReminderIntervalMinutes = 60,
        TimeOnly? waterWindowStartUtc = null,
        TimeOnly? waterWindowEndUtc = null,
        bool weeklySummaryEnabled = true,
        DayOfWeek weeklySummaryDayOfWeekUtc = DayOfWeek.Sunday,
        TimeOnly? weeklySummaryTimeOfDayUtc = null,
        bool goalAlertsEnabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var startUtc = waterWindowStartUtc ?? new TimeOnly(6, 0);
        var endUtc = waterWindowEndUtc ?? new TimeOnly(22, 0);
        var summaryTimeUtc = weeklySummaryTimeOfDayUtc ?? new TimeOnly(8, 0);

        EnsureValid(
            mealReminderLeadTimeMinutes,
            mealMissedGraceMinutes,
            waterReminderIntervalMinutes,
            startUtc,
            endUtc);

        return new DietReminderSettings
        {
            Id = id,
            UserId = userId,
            MealRemindersEnabled = mealRemindersEnabled,
            MealReminderLeadTimeMinutes = mealReminderLeadTimeMinutes,
            MealMissedGraceMinutes = mealMissedGraceMinutes,
            WaterRemindersEnabled = waterRemindersEnabled,
            WaterReminderIntervalMinutes = waterReminderIntervalMinutes,
            WaterWindowStartUtc = startUtc,
            WaterWindowEndUtc = endUtc,
            WeeklySummaryEnabled = weeklySummaryEnabled,
            WeeklySummaryDayOfWeekUtc = weeklySummaryDayOfWeekUtc,
            WeeklySummaryTimeOfDayUtc = summaryTimeUtc,
            GoalAlertsEnabled = goalAlertsEnabled,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public string UserId { get; private set; } = default!;
    public bool MealRemindersEnabled { get; private set; }
    public int MealReminderLeadTimeMinutes { get; private set; }
    public int MealMissedGraceMinutes { get; private set; }
    public bool WaterRemindersEnabled { get; private set; }
    public int WaterReminderIntervalMinutes { get; private set; }
    public TimeOnly WaterWindowStartUtc { get; private set; }
    public TimeOnly WaterWindowEndUtc { get; private set; }
    public bool WeeklySummaryEnabled { get; private set; }
    public DayOfWeek WeeklySummaryDayOfWeekUtc { get; private set; }
    public TimeOnly WeeklySummaryTimeOfDayUtc { get; private set; }
    public bool GoalAlertsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Update(
        bool mealRemindersEnabled,
        int mealReminderLeadTimeMinutes,
        int mealMissedGraceMinutes,
        bool waterRemindersEnabled,
        int waterReminderIntervalMinutes,
        TimeOnly waterWindowStartUtc,
        TimeOnly waterWindowEndUtc,
        bool weeklySummaryEnabled,
        DayOfWeek weeklySummaryDayOfWeekUtc,
        TimeOnly weeklySummaryTimeOfDayUtc,
        bool goalAlertsEnabled)
    {
        EnsureValid(
            mealReminderLeadTimeMinutes,
            mealMissedGraceMinutes,
            waterReminderIntervalMinutes,
            waterWindowStartUtc,
            waterWindowEndUtc);

        MealRemindersEnabled = mealRemindersEnabled;
        MealReminderLeadTimeMinutes = mealReminderLeadTimeMinutes;
        MealMissedGraceMinutes = mealMissedGraceMinutes;
        WaterRemindersEnabled = waterRemindersEnabled;
        WaterReminderIntervalMinutes = waterReminderIntervalMinutes;
        WaterWindowStartUtc = waterWindowStartUtc;
        WaterWindowEndUtc = waterWindowEndUtc;
        WeeklySummaryEnabled = weeklySummaryEnabled;
        WeeklySummaryDayOfWeekUtc = weeklySummaryDayOfWeekUtc;
        WeeklySummaryTimeOfDayUtc = weeklySummaryTimeOfDayUtc;
        GoalAlertsEnabled = goalAlertsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureValid(
        int mealReminderLeadTimeMinutes,
        int mealMissedGraceMinutes,
        int waterReminderIntervalMinutes,
        TimeOnly waterWindowStartUtc,
        TimeOnly waterWindowEndUtc)
    {
        if (mealReminderLeadTimeMinutes <= 0)
            throw new DietPlannerDomainException("MealReminderLeadTimeMinutes must be greater than zero.");

        if (mealMissedGraceMinutes <= 0)
            throw new DietPlannerDomainException("MealMissedGraceMinutes must be greater than zero.");

        if (waterReminderIntervalMinutes <= 0)
            throw new DietPlannerDomainException("WaterReminderIntervalMinutes must be greater than zero.");

        if (waterWindowEndUtc <= waterWindowStartUtc)
            throw new DietPlannerDomainException("WaterWindowEndUtc must be after WaterWindowStartUtc.");
    }
}
