namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

internal sealed class WaterReminderCandidateQueries(
    DietPlannerDbContext dbContext,
    IOptions<DietReminderTickServiceOptions> options)
    : IWaterReminderCandidateQueries
{
    private const string DefaultLocale = "en";

    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public async Task<IReadOnlyList<WaterReminderCandidate>> GetCandidatesAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.WaterRemindersEnabled
            join state in dbContext.WaterReminderStates.AsNoTracking()
                on settings.PersonId equals state.PersonId into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            select new
            {
                settings.PersonId,
                settings.WaterReminderIntervalMinutes,
                settings.WaterWindowStart,
                settings.WaterWindowEnd,
                LastAt = state == null ? (DateTime?)null : state.LastWaterReminderAt
            }).ToListAsync(ct);

        return rows
            .Select(r => new WaterReminderCandidate(
                r.PersonId,
                DefaultLocale,
                _timeZone,
                r.WaterReminderIntervalMinutes,
                r.WaterWindowStart,
                r.WaterWindowEnd,
                r.LastAt))
            .ToList();
    }
}
