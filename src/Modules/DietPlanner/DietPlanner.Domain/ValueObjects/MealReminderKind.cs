namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Which notification a <c>SentMealReminder</c> ledger row records for a meal entry, so each kind is
/// sent at most once per entry. Values are persisted as integers — do not renumber.
/// </summary>
public enum MealReminderKind
{
    /// <summary>The "meal coming up" reminder sent shortly before the planned time.</summary>
    Reminder = 1,

    /// <summary>The "meal missed" notice sent after the planned time passed without completion.</summary>
    Missed = 2
}
