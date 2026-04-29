namespace DietPlanner.Application.Workers;

internal interface IWaterReminderCandidateQueries
{
    /// <summary>
    /// Window/interval filtering is performed in the job.
    /// </summary>
    Task<IReadOnlyList<WaterReminderCandidate>> GetCandidatesAsync(
        DateTime nowUtc, CancellationToken ct);
}
