namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class WaterReminderCandidateQueries(DietPlannerDbContext dbContext)
    : IWaterReminderCandidateQueries
{
    private const string DefaultLocale = "en";

    public async Task<IReadOnlyList<WaterReminderCandidate>> GetCandidatesAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.WaterRemindersEnabled
            join state in dbContext.WaterReminderStates.AsNoTracking()
                on settings.UserId equals state.UserId into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            select new
            {
                settings.UserId,
                settings.WaterReminderIntervalMinutes,
                settings.WaterWindowStartUtc,
                settings.WaterWindowEndUtc,
                LastAt = state == null ? (DateTime?)null : state.LastWaterReminderAt
            }).ToListAsync(ct);

        return rows
            .Select(r => new WaterReminderCandidate(
                r.UserId,
                DefaultLocale,
                r.WaterReminderIntervalMinutes,
                r.WaterWindowStartUtc,
                r.WaterWindowEndUtc,
                r.LastAt))
            .ToList();
    }
}
