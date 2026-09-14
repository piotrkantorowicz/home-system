namespace DietPlanner.Domain.Ledgers;

/// <summary>
/// Per-user ledger of the last water reminder, so the reminder job honours the configured interval
/// across ticks and restarts. Not an aggregate — one flat row per user.
/// </summary>
public sealed class WaterReminderState
{
    private WaterReminderState() { }

    /// <summary>Creates the row for a user the first time a reminder is sent.</summary>
    /// <param name="userId">Auth subject of the user; required.</param>
    /// <param name="lastReminderAt">When the reminder was sent, UTC.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    public static WaterReminderState Create(string userId, DateTime lastReminderAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WaterReminderState
        {
            UserId = userId,
            LastWaterReminderAt = lastReminderAt
        };
    }

    /// <summary>Auth subject of the user; the key.</summary>
    public string UserId { get; private init; } = default!;
    /// <summary>When the last water reminder was sent, UTC.</summary>
    public DateTime? LastWaterReminderAt { get; private set; }

    /// <summary>Advances the ledger after another reminder is sent.</summary>
    /// <param name="sentAt">When it was sent, UTC.</param>
    public void UpdateLastReminderAt(DateTime sentAt)
        => LastWaterReminderAt = sentAt;
}
