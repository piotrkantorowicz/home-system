namespace DietPlanner.Application.Workers;

internal interface IWeeklySummaryCandidateQueries
{
    /// <summary>
    /// Returns all users with WeeklySummaryEnabled = true, joined with dedup state.
    /// Day/time/dedup filtering is performed in the job.
    /// </summary>
    Task<IReadOnlyList<WeeklySummaryCandidate>> GetCandidatesAsync(CancellationToken ct);

    /// <summary>
    /// Computes the summary stats for a user over the given calendar week.
    /// </summary>
    Task<WeeklyStats> GetStatsAsync(Guid personId, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct);
}
