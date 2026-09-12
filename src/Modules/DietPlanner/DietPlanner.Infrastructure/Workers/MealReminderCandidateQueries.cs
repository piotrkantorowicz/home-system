namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class MealReminderCandidateQueries(DietPlannerDbContext dbContext)
    : IMealReminderCandidateQueries
{
    private const string DefaultLocale = "en";
    private static readonly TimeSpan MissedLookback = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<MealReminderCandidate>> GetDueRemindersAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var nowDate = DateOnly.FromDateTime(nowUtc);
        var horizonDate = DateOnly.FromDateTime(nowUtc.AddDays(1));

        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.MealRemindersEnabled
            join entry in dbContext.MealEntries.AsNoTracking()
                on settings.UserId equals entry.UserId
            join slot in dbContext.MealSlots.AsNoTracking()
                on entry.MealSlotId equals slot.Id
            where entry.Status == MealEntryStatus.Planned
                && entry.Date >= nowDate && entry.Date <= horizonDate
                && !dbContext.SentMealReminders.AsNoTracking()
                    .Any(s => s.MealEntryId == entry.Id && s.Kind == MealReminderKind.Reminder)
            select new
            {
                settings.UserId,
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
            var plannedAt = DateTime.SpecifyKind(r.Date.ToDateTime(time), DateTimeKind.Utc);
            var leadEnd = nowUtc.AddMinutes(r.MealReminderLeadTimeMinutes);
            if (plannedAt > nowUtc && plannedAt <= leadEnd)
            {
                result.Add(new MealReminderCandidate(
                    r.UserId, DefaultLocale, r.EntryId.Value, r.SlotName, plannedAt));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<MealReminderCandidate>> GetMissedRemindersAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var lowerDate = DateOnly.FromDateTime(nowUtc.Subtract(MissedLookback));
        var upperDate = DateOnly.FromDateTime(nowUtc);

        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.MealRemindersEnabled
            join entry in dbContext.MealEntries.AsNoTracking()
                on settings.UserId equals entry.UserId
            join slot in dbContext.MealSlots.AsNoTracking()
                on entry.MealSlotId equals slot.Id
            where entry.Status == MealEntryStatus.Planned
                && entry.Date >= lowerDate && entry.Date <= upperDate
                && !dbContext.SentMealReminders.AsNoTracking()
                    .Any(s => s.MealEntryId == entry.Id && s.Kind == MealReminderKind.Missed)
            select new
            {
                settings.UserId,
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
            var plannedAt = DateTime.SpecifyKind(r.Date.ToDateTime(time), DateTimeKind.Utc);
            var missedAt = plannedAt.AddMinutes(r.MealMissedGraceMinutes);
            if (missedAt <= nowUtc && plannedAt > nowUtc.Subtract(MissedLookback))
            {
                result.Add(new MealReminderCandidate(
                    r.UserId, DefaultLocale, r.EntryId.Value, r.SlotName, plannedAt));
            }
        }

        return result;
    }
}
