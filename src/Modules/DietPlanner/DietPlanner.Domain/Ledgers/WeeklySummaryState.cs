namespace DietPlanner.Domain.Ledgers;

/// <summary>
/// Per-user ledger of the last weekly summary, so the summary job sends at most one per configured
/// week across ticks and restarts. Not an aggregate — one flat row per user.
/// </summary>
public sealed class WeeklySummaryState
{
    private WeeklySummaryState() { }

    /// <summary>Creates the row for a user the first time a summary is sent.</summary>
    /// <param name="userId">Auth subject of the user; required.</param>
    /// <param name="lastSummaryAt">When the summary was sent, UTC.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    public static WeeklySummaryState Create(string userId, DateTime lastSummaryAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WeeklySummaryState
        {
            UserId = userId,
            LastWeeklySummaryAt = lastSummaryAt
        };
    }

    /// <summary>Auth subject of the user; the key.</summary>
    public string UserId { get; private init; } = default!;
    /// <summary>When the last weekly summary was sent, UTC.</summary>
    public DateTime? LastWeeklySummaryAt { get; private set; }

    /// <summary>Advances the ledger after another summary is sent.</summary>
    /// <param name="sentAt">When it was sent, UTC.</param>
    public void UpdateLastSummaryAt(DateTime sentAt)
        => LastWeeklySummaryAt = sentAt;
}
