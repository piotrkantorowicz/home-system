namespace DietPlanner.Domain.Ledgers;

using DietPlanner.Domain.ValueObjects;

public sealed class SentMealReminder
{
    private SentMealReminder() { }

    public static SentMealReminder Create(MealEntryId mealEntryId, MealReminderKind kind, DateTime sentAtUtc)
    {
        ArgumentNullException.ThrowIfNull(mealEntryId);
        return new SentMealReminder
        {
            MealEntryId = mealEntryId,
            Kind = kind,
            SentAt = sentAtUtc
        };
    }

    public MealEntryId MealEntryId { get; private set; } = default!;
    public MealReminderKind Kind { get; private set; }
    public DateTime SentAt { get; private set; }
}
