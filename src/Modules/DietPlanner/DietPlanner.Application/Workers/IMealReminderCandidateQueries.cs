namespace DietPlanner.Application.Workers;

internal interface IMealReminderCandidateQueries
{
    /// <summary>
    /// Returns meals where computed PlannedAt ∈ (nowUtc, nowUtc + lead]
    /// for users whose DietReminderSettings has MealRemindersEnabled = true,
    /// excluding rows already present in sent_meal_reminders with kind = Reminder.
    /// </summary>
    Task<IReadOnlyList<MealReminderCandidate>> GetDueRemindersAsync(DateTime nowUtc, CancellationToken ct);

    /// <summary>
    /// Returns meals where computed PlannedAt + grace ≤ nowUtc and PlannedAt > nowUtc - 24h
    /// and Status != Done, for users with MealRemindersEnabled = true,
    /// excluding rows already in sent_meal_reminders with kind = Missed.
    /// </summary>
    Task<IReadOnlyList<MealReminderCandidate>> GetMissedRemindersAsync(DateTime nowUtc, CancellationToken ct);
}
