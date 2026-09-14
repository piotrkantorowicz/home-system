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

    /// <summary>Records that a reminder of the given kind was sent for the entry.</summary>
    /// <param name="mealEntryId">The meal entry the notification was about.</param>
    /// <param name="kind">Which notification was sent.</param>
    /// <param name="sentAtUtc">When it was sent, UTC.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mealEntryId"/> is null.</exception>
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

    /// <summary>The meal entry the notification was about; half of the key.</summary>
    public MealEntryId MealEntryId { get; private init; } = default!;
    /// <summary>Which notification was sent; the other half of the key.</summary>
    public MealReminderKind Kind { get; private init; }
    /// <summary>When it was sent, UTC.</summary>
    public DateTime SentAt { get; private init; }
}
