namespace DietPlanner.Domain.Ledgers;

using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Idempotency ledger for meal reminder publication. NOT a DDD aggregate root —
/// flat dedup row keyed on (MealEntryId, Kind). Filed under Domain/Ledgers/ to
/// signal it is owned by the bounded context but does not participate in the
/// aggregate model.
/// </summary>
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

    public MealEntryId MealEntryId { get; private init; } = default!;
    public MealReminderKind Kind { get; private init; }
    public DateTime SentAt { get; private init; }
}
