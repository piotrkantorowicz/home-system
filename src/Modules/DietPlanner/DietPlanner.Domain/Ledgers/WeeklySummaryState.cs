namespace DietPlanner.Domain.Ledgers;

/// <summary>
/// Per-user ledger of the last weekly summary, so the summary job sends at most one per configured
/// week across ticks and restarts. Not an aggregate — one flat row per user.
/// </summary>
public sealed class WeeklySummaryState
{
    private WeeklySummaryState() { }

    /// <summary>Creates the row for a user the first time a summary is sent.</summary>
    /// <param name="personId">Person identifier of the user; required.</param>
    /// <param name="lastSummaryAt">When the summary was sent, UTC.</param>
    /// <exception cref="ArgumentException"><paramref name="personId"/> is empty.</exception>
    public static WeeklySummaryState Create(Guid personId, DateTime lastSummaryAt)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));
        return new WeeklySummaryState
        {
            PersonId = personId,
            LastWeeklySummaryAt = lastSummaryAt
        };
    }

    /// <summary>Person identifier of the user; the key.</summary>
    public Guid PersonId { get; private init; }
    /// <summary>When the last weekly summary was sent, UTC.</summary>
    public DateTime? LastWeeklySummaryAt { get; private set; }

    /// <summary>Advances the ledger after another summary is sent.</summary>
    /// <param name="sentAt">When it was sent, UTC.</param>
    public void UpdateLastSummaryAt(DateTime sentAt)
        => LastWeeklySummaryAt = sentAt;
}
