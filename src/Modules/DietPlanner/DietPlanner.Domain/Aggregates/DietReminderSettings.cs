namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A user's notification preferences for the Diet Planner: meal reminders (lead time before the
/// planned time, grace period before a meal counts as missed), water reminders (interval and the
/// window they may fire in), the weekly summary (day and time) and goal alerts. One row per user,
/// created with defaults on first access. Times of day are local wall-clock values; the reminder
/// jobs read them in the configured time zone, so they stay put across DST changes.
/// </summary>
public sealed class DietReminderSettings : AggregateRoot<DietReminderSettingsId>
{
    private DietReminderSettings() { }

    /// <summary>
    /// Creates the settings. Defaults: meal reminders 15 min ahead / missed after 30 min; water every
    /// 60 min between 06:00 and 22:00; weekly summary Sunday 08:00; everything enabled.
    /// </summary>
    /// <param name="id">Identifier for the new settings.</param>
    /// <param name="personId">Person identifier of the owner; required.</param>
    /// <param name="mealRemindersEnabled">Whether "meal coming up" and "meal missed" notifications fire.</param>
    /// <param name="mealReminderLeadTimeMinutes">Minutes before the planned time to remind; positive.</param>
    /// <param name="mealMissedGraceMinutes">Minutes after the planned time before a meal counts as missed; positive.</param>
    /// <param name="waterRemindersEnabled">Whether water reminders fire.</param>
    /// <param name="waterReminderIntervalMinutes">Minimum minutes between water reminders; positive.</param>
    /// <param name="waterWindowStart">Earliest local time of day a water reminder may fire; 06:00 if omitted.</param>
    /// <param name="waterWindowEnd">Latest local time of day a water reminder may fire; 22:00 if omitted, must be after the start.</param>
    /// <param name="weeklySummaryEnabled">Whether the weekly summary is sent.</param>
    /// <param name="weeklySummaryDayOfWeek">Local weekday the summary is sent on.</param>
    /// <param name="weeklySummaryTimeOfDay">Local time of day the summary is sent at; 08:00 if omitted.</param>
    /// <param name="goalAlertsEnabled">Whether goal milestone notifications fire.</param>
    /// <exception cref="ArgumentException"><paramref name="personId"/> is empty.</exception>
    /// <exception cref="DietPlannerDomainException">A minute value is not positive or the water window is empty.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static DietReminderSettings Create(
        DietReminderSettingsId id,
        Guid personId,
        DateTime now,
        bool mealRemindersEnabled = true,
        int mealReminderLeadTimeMinutes = 15,
        int mealMissedGraceMinutes = 30,
        bool waterRemindersEnabled = true,
        int waterReminderIntervalMinutes = 60,
        TimeOnly? waterWindowStart = null,
        TimeOnly? waterWindowEnd = null,
        bool weeklySummaryEnabled = true,
        DayOfWeek weeklySummaryDayOfWeek = DayOfWeek.Sunday,
        TimeOnly? weeklySummaryTimeOfDay = null,
        bool goalAlertsEnabled = true)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        var start = waterWindowStart ?? new TimeOnly(6, 0);
        var end = waterWindowEnd ?? new TimeOnly(22, 0);
        var summaryTime = weeklySummaryTimeOfDay ?? new TimeOnly(8, 0);

        EnsureValid(
            mealReminderLeadTimeMinutes,
            mealMissedGraceMinutes,
            waterReminderIntervalMinutes,
            start,
            end);

        return new DietReminderSettings
        {
            Id = id,
            PersonId = personId,
            MealRemindersEnabled = mealRemindersEnabled,
            MealReminderLeadTimeMinutes = mealReminderLeadTimeMinutes,
            MealMissedGraceMinutes = mealMissedGraceMinutes,
            WaterRemindersEnabled = waterRemindersEnabled,
            WaterReminderIntervalMinutes = waterReminderIntervalMinutes,
            WaterWindowStart = start,
            WaterWindowEnd = end,
            WeeklySummaryEnabled = weeklySummaryEnabled,
            WeeklySummaryDayOfWeek = weeklySummaryDayOfWeek,
            WeeklySummaryTimeOfDay = summaryTime,
            GoalAlertsEnabled = goalAlertsEnabled,
            CreatedAt = now,
        };
    }

    /// <summary>Person identifier of the owner.</summary>
    public Guid PersonId { get; private set; }
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
    /// <summary>Earliest local time of day a water reminder may fire.</summary>
    public TimeOnly WaterWindowStart { get; private set; }
    /// <summary>Latest local time of day a water reminder may fire; always after the start.</summary>
    public TimeOnly WaterWindowEnd { get; private set; }
    /// <summary>Whether the weekly summary is sent.</summary>
    public bool WeeklySummaryEnabled { get; private set; }
    /// <summary>Local weekday the weekly summary is sent on.</summary>
    public DayOfWeek WeeklySummaryDayOfWeek { get; private set; }
    /// <summary>Local time of day the weekly summary is sent at.</summary>
    public TimeOnly WeeklySummaryTimeOfDay { get; private set; }
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
    /// <param name="waterWindowStart">Earliest local time of day a water reminder may fire.</param>
    /// <param name="waterWindowEnd">Latest local time of day a water reminder may fire; must be after the start.</param>
    /// <param name="weeklySummaryEnabled">Whether the weekly summary is sent.</param>
    /// <param name="weeklySummaryDayOfWeek">Local weekday the summary is sent on.</param>
    /// <param name="weeklySummaryTimeOfDay">Local time of day the summary is sent at.</param>
    /// <param name="goalAlertsEnabled">Whether goal milestone notifications fire.</param>
    /// <exception cref="DietPlannerDomainException">A minute value is not positive or the water window is empty.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public void Update(
        bool mealRemindersEnabled,
        int mealReminderLeadTimeMinutes,
        int mealMissedGraceMinutes,
        bool waterRemindersEnabled,
        int waterReminderIntervalMinutes,
        TimeOnly waterWindowStart,
        TimeOnly waterWindowEnd,
        bool weeklySummaryEnabled,
        DayOfWeek weeklySummaryDayOfWeek,
        TimeOnly weeklySummaryTimeOfDay,
        bool goalAlertsEnabled,
        DateTime now)
    {
        EnsureValid(
            mealReminderLeadTimeMinutes,
            mealMissedGraceMinutes,
            waterReminderIntervalMinutes,
            waterWindowStart,
            waterWindowEnd);

        MealRemindersEnabled = mealRemindersEnabled;
        MealReminderLeadTimeMinutes = mealReminderLeadTimeMinutes;
        MealMissedGraceMinutes = mealMissedGraceMinutes;
        WaterRemindersEnabled = waterRemindersEnabled;
        WaterReminderIntervalMinutes = waterReminderIntervalMinutes;
        WaterWindowStart = waterWindowStart;
        WaterWindowEnd = waterWindowEnd;
        WeeklySummaryEnabled = weeklySummaryEnabled;
        WeeklySummaryDayOfWeek = weeklySummaryDayOfWeek;
        WeeklySummaryTimeOfDay = weeklySummaryTimeOfDay;
        GoalAlertsEnabled = goalAlertsEnabled;
        UpdatedAt = now;
    }

    private static void EnsureValid(
        int mealReminderLeadTimeMinutes,
        int mealMissedGraceMinutes,
        int waterReminderIntervalMinutes,
        TimeOnly waterWindowStart,
        TimeOnly waterWindowEnd)
    {
        if (mealReminderLeadTimeMinutes <= 0)
            throw new DietPlannerDomainException("MealReminderLeadTimeMinutes must be greater than zero.");

        if (mealMissedGraceMinutes <= 0)
            throw new DietPlannerDomainException("MealMissedGraceMinutes must be greater than zero.");

        if (waterReminderIntervalMinutes <= 0)
            throw new DietPlannerDomainException("WaterReminderIntervalMinutes must be greater than zero.");

        if (waterWindowEnd <= waterWindowStart)
            throw new DietPlannerDomainException("WaterWindowEnd must be after WaterWindowStart.");
    }
}
