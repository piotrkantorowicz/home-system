namespace DietPlanner.Domain.Ledgers;

public sealed class WeeklySummaryState
{
    private WeeklySummaryState() { }

    public static WeeklySummaryState Create(string userId, DateTime lastSummaryAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WeeklySummaryState
        {
            UserId = userId,
            LastWeeklySummaryAt = lastSummaryAt
        };
    }

    public string UserId { get; private init; } = default!;
    public DateTime? LastWeeklySummaryAt { get; private set; }

    public void UpdateLastSummaryAt(DateTime sentAt)
        => LastWeeklySummaryAt = sentAt;
}
