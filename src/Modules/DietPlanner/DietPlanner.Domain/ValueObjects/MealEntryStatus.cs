namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Lifecycle of a <c>MealEntry</c>. An entry starts <see cref="Planned"/>; completing it marks it
/// <see cref="Done"/> when eaten as planned or <see cref="Modified"/> when the actual products differ
/// from the plan. Values are persisted as integers — do not renumber.
/// </summary>
public enum MealEntryStatus
{
    /// <summary>Scheduled but not yet eaten; still counted as upcoming by reminders.</summary>
    Planned = 0,

    /// <summary>Eaten as planned; macros come from the planned recipe.</summary>
    Done = 1,

    /// <summary>Eaten with substitutions; macros come from the recorded actual products.</summary>
    Modified = 2
}
