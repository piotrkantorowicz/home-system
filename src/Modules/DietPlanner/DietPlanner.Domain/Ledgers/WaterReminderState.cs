namespace DietPlanner.Domain.Ledgers;

public sealed class WaterReminderState
{
    private WaterReminderState() { }

    public static WaterReminderState Create(string userId, DateTime lastReminderAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WaterReminderState
        {
            UserId = userId,
            LastWaterReminderAt = lastReminderAt
        };
    }

    public string UserId { get; private init; } = default!;
    public DateTime? LastWaterReminderAt { get; private set; }

    public void UpdateLastReminderAt(DateTime sentAt)
        => LastWaterReminderAt = sentAt;
}
