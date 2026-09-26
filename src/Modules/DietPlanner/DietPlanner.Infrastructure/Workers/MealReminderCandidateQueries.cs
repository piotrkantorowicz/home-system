namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

internal sealed class MealReminderCandidateQueries(
    DietPlannerDbContext dbContext,
    IOptions<DietReminderTickServiceOptions> options)
    : IMealReminderCandidateQueries
{
    private const string DefaultLocale = "en";
    private static readonly TimeSpan MissedLookback = TimeSpan.FromHours(24);

    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public async Task<IReadOnlyList<MealReminderCandidate>> GetDueRemindersAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var nowDate = LocalDate(nowUtc);
        var horizonDate = nowDate.AddDays(1);

        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.MealRemindersEnabled
            join entry in dbContext.MealEntries.AsNoTracking()
                on settings.PersonId equals entry.PersonId
            join slot in dbContext.MealSlots.AsNoTracking()
                on entry.MealSlotId equals slot.Id
            where entry.Status == MealEntryStatus.Planned
                && entry.Date >= nowDate && entry.Date <= horizonDate
                && !dbContext.SentMealReminders.AsNoTracking()
                    .Any(s => s.MealEntryId == entry.Id && s.Kind == MealReminderKind.Reminder)
            select new
            {
                settings.PersonId,
                settings.MealReminderLeadTimeMinutes,
                EntryId = entry.Id,
                entry.Date,
                entry.MealTime,
                SlotName = slot.Name,
                SlotDefaultTime = slot.DefaultTime
            }).ToListAsync(ct);

        var result = new List<MealReminderCandidate>(rows.Count);
        foreach (var r in rows)
        {
            var time = r.MealTime ?? r.SlotDefaultTime;
            var plannedAt = ToUtc(r.Date.ToDateTime(time));
            var leadEnd = nowUtc.AddMinutes(r.MealReminderLeadTimeMinutes);
            if (plannedAt > nowUtc && plannedAt <= leadEnd)
            {
                result.Add(new MealReminderCandidate(
                    r.PersonId, DefaultLocale, r.EntryId.Value, r.SlotName, plannedAt));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<MealReminderCandidate>> GetMissedRemindersAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var lowerDate = LocalDate(nowUtc.Subtract(MissedLookback));
        var upperDate = LocalDate(nowUtc);

        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.MealRemindersEnabled
            join entry in dbContext.MealEntries.AsNoTracking()
                on settings.PersonId equals entry.PersonId
            join slot in dbContext.MealSlots.AsNoTracking()
                on entry.MealSlotId equals slot.Id
            where entry.Status == MealEntryStatus.Planned
                && entry.Date >= lowerDate && entry.Date <= upperDate
                && !dbContext.SentMealReminders.AsNoTracking()
                    .Any(s => s.MealEntryId == entry.Id && s.Kind == MealReminderKind.Missed)
            select new
            {
                settings.PersonId,
                settings.MealMissedGraceMinutes,
                EntryId = entry.Id,
                entry.Date,
                entry.MealTime,
                SlotName = slot.Name,
                SlotDefaultTime = slot.DefaultTime
            }).ToListAsync(ct);

        var result = new List<MealReminderCandidate>(rows.Count);
        foreach (var r in rows)
        {
            var time = r.MealTime ?? r.SlotDefaultTime;
            var plannedAt = ToUtc(r.Date.ToDateTime(time));
            var missedAt = plannedAt.AddMinutes(r.MealMissedGraceMinutes);
            if (missedAt <= nowUtc && plannedAt > nowUtc.Subtract(MissedLookback))
            {
                result.Add(new MealReminderCandidate(
                    r.PersonId, DefaultLocale, r.EntryId.Value, r.SlotName, plannedAt));
            }
        }

        return result;
    }

    private DateOnly LocalDate(DateTime utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone));

    // A meal planned inside the spring-forward gap (02:30 on the change night) has no UTC instant;
    // it is pushed forward past the gap (02:30 → 03:30). Ambiguous fall-back times take standard time.
    private DateTime ToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(_timeZone.IsInvalidTime(local) ? local.AddHours(1) : local, _timeZone);
}
