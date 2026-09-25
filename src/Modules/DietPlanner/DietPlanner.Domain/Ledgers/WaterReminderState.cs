namespace DietPlanner.Domain.Ledgers;

/// <summary>
/// Per-user ledger of the last water reminder, so the reminder job honours the configured interval
/// across ticks and restarts. Not an aggregate — one flat row per user.
/// </summary>
public sealed class WaterReminderState
{
    private WaterReminderState() { }

    /// <summary>Creates the row for a user the first time a reminder is sent.</summary>
    /// <param name="personId">Person identifier of the user; required.</param>
    /// <param name="lastReminderAt">When the reminder was sent, UTC.</param>
    /// <exception cref="ArgumentException"><paramref name="personId"/> is empty.</exception>
    public static WaterReminderState Create(Guid personId, DateTime lastReminderAt)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));
        return new WaterReminderState
        {
            PersonId = personId,
            LastWaterReminderAt = lastReminderAt
        };
    }

    /// <summary>Person identifier of the user; the key.</summary>
    public Guid PersonId { get; private init; }
    /// <summary>When the last water reminder was sent, UTC.</summary>
    public DateTime? LastWaterReminderAt { get; private set; }

    /// <summary>Advances the ledger after another reminder is sent.</summary>
    /// <param name="sentAt">When it was sent, UTC.</param>
    public void UpdateLastReminderAt(DateTime sentAt)
        => LastWaterReminderAt = sentAt;
}
