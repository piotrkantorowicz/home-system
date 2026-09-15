namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A user's notification preferences for the Diet Planner: meal reminders (lead time before the
/// planned time, grace period before a meal counts as missed), water reminders (interval and the
/// UTC window they may fire in), the weekly summary (UTC day and time) and goal alerts. One row per
/// user, created with defaults on first access. All times are UTC — the user's locale is applied
/// when the notification is rendered, not here.
/// </summary>
public sealed class DietReminderSettings : AggregateRoot<DietReminderSettingsId>
{
    private DietReminderSettings() { }

    /// <summary>
    /// Creates the settings. Defaults: meal reminders 15 min ahead / missed after 30 min; water every
    /// 60 min between 06:00 and 22:00 UTC; weekly summary Sunday 08:00 UTC; everything enabled.
    /// </summary>
    /// <param name="id">Identifier for the new settings.</param>
    /// <param name="userId">Auth subject of the owner; required.</param>
    /// <param name="mealRemindersEnabled">Whether "meal coming up" and "meal missed" notifications fire.</param>
    /// <param name="mealReminderLeadTimeMinutes">Minutes before the planned time to remind; positive.</param>
    /// <param name="mealMissedGraceMinutes">Minutes after the planned time before a meal counts as missed; positive.</param>
    /// <param name="waterRemindersEnabled">Whether water reminders fire.</param>
    /// <param name="waterReminderIntervalMinutes">Minimum minutes between water reminders; positive.</param>
    /// <param name="waterWindowStartUtc">Earliest UTC time of day a water reminder may fire; 06:00 if omitted.</param>
    /// <param name="waterWindowEndUtc">Latest UTC time of day a water reminder may fire; 22:00 if omitted, must be after the start.</param>
    /// <param name="weeklySummaryEnabled">Whether the weekly summary is sent.</param>
    /// <param name="weeklySummaryDayOfWeekUtc">UTC weekday the summary is sent on.</param>
    /// <param name="weeklySummaryTimeOfDayUtc">UTC time of day the summary is sent at; 08:00 if omitted.</param>
    /// <param name="goalAlertsEnabled">Whether goal milestone notifications fire.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    /// <exception cref="DietPlannerDomainException">A minute value is not positive or the water window is empty.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static DietReminderSettings Create(
        DietReminderSettingsId id,
        string userId,
        DateTime now,
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
            CreatedAt = now,
        };
    }

    /// <summary>Auth subject of the owner.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>Whether "meal coming up" and "meal missed" notifications fire.</summary>
    public bool MealRemindersEnabled { get; private set; }
    /// <summary>Minutes before the planned time the reminder is sent.</summary>
    public int MealReminderLeadTimeMinutes { get; private set; }
    /// <summary>Minutes after the planned time before an uncompleted meal counts as missed.</summary>
    public int MealMissedGraceMinutes { get; private set; }
    /// <summary>Whether water reminders fire.</summary>
    public bool WaterRemindersEnabled { get; private set; }
    /// <summary>Minimum minutes between two water reminders.</summary>
    public int WaterReminderIntervalMinutes { get; private set; }
    /// <summary>Earliest UTC time of day a water reminder may fire.</summary>
    public TimeOnly WaterWindowStartUtc { get; private set; }
    /// <summary>Latest UTC time of day a water reminder may fire; always after the start.</summary>
    public TimeOnly WaterWindowEndUtc { get; private set; }
    /// <summary>Whether the weekly summary is sent.</summary>
    public bool WeeklySummaryEnabled { get; private set; }
    /// <summary>UTC weekday the weekly summary is sent on.</summary>
    public DayOfWeek WeeklySummaryDayOfWeekUtc { get; private set; }
    /// <summary>UTC time of day the weekly summary is sent at.</summary>
    public TimeOnly WeeklySummaryTimeOfDayUtc { get; private set; }
    /// <summary>Whether goal milestone notifications fire.</summary>
    public bool GoalAlertsEnabled { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last <see cref="Update"/>, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Replaces every preference at once, re-validating the same rules as creation.</summary>
    /// <param name="mealRemindersEnabled">Whether meal notifications fire.</param>
    /// <param name="mealReminderLeadTimeMinutes">Minutes before the planned time to remind; positive.</param>
    /// <param name="mealMissedGraceMinutes">Minutes after the planned time before a meal counts as missed; positive.</param>
    /// <param name="waterRemindersEnabled">Whether water reminders fire.</param>
    /// <param name="waterReminderIntervalMinutes">Minimum minutes between water reminders; positive.</param>
    /// <param name="waterWindowStartUtc">Earliest UTC time of day a water reminder may fire.</param>
    /// <param name="waterWindowEndUtc">Latest UTC time of day a water reminder may fire; must be after the start.</param>
    /// <param name="weeklySummaryEnabled">Whether the weekly summary is sent.</param>
    /// <param name="weeklySummaryDayOfWeekUtc">UTC weekday the summary is sent on.</param>
    /// <param name="weeklySummaryTimeOfDayUtc">UTC time of day the summary is sent at.</param>
    /// <param name="goalAlertsEnabled">Whether goal milestone notifications fire.</param>
    /// <exception cref="DietPlannerDomainException">A minute value is not positive or the water window is empty.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
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
        bool goalAlertsEnabled,
        DateTime now)
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
        UpdatedAt = now;
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
