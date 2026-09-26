namespace DietPlanner.Application.Workers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

internal sealed class WeeklySummaryJob(
    IWeeklySummaryCandidateQueries queries,
    IWeeklySummaryStateRepository stateRepo,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork,
    IHouseholdQueryService persons) : IDietReminderJob
{
    public string Name => "WeeklySummaryJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<WeeklySummaryCandidate> candidates = await queries.GetCandidatesAsync(ct);

        var publishedAny = false;

        foreach (var c in candidates)
        {
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, c.TimeZone);

            if (nowLocal.DayOfWeek != c.WeeklySummaryDayOfWeek)
                continue;

            if (TimeOnly.FromDateTime(nowLocal) < c.WeeklySummaryTimeOfDay)
                continue;

            var today = DateOnly.FromDateTime(nowLocal);
            if (!IsWeekElapsed(today, c.LastWeeklySummaryAt, c.TimeZone))
                continue;

            var weekEnd = today.AddDays(-1);
            var weekStart = weekEnd.AddDays(-6);

            WeeklyStats stats = await queries.GetStatsAsync(c.PersonId, weekStart, weekEnd, ct);

            string? authSubject = await persons.GetAuthSubjectForPersonAsync(c.PersonId, ct);
            if (authSubject is null) continue;

            var existing = await stateRepo.GetByPersonIdAsync(c.PersonId, ct);
            if (existing is null)
                await stateRepo.AddAsync(WeeklySummaryState.Create(c.PersonId, nowUtc), ct);
            else
                existing.UpdateLastSummaryAt(nowUtc);

            await bus.PublishAsync(new WeeklySummaryDueIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: nowUtc,
                UserId: authSubject,
                Locale: c.Locale,
                WeekStart: weekStart,
                WeekEnd: weekEnd,
                TotalKcal: stats.TotalKcal,
                TargetKcal: stats.TargetKcal,
                AvgWaterLiters: stats.AvgWaterLiters,
                WeightDeltaKg: stats.WeightDeltaKg,
                MealsCompleted: stats.MealsCompleted,
                MealsPlanned: stats.MealsPlanned), ct);

            publishedAny = true;
        }

        if (publishedAny)
            await unitOfWork.CommitAsync(ct);
    }

    // Compared on local dates, not UTC instants: across a DST change the same local send time is
    // 7 days ± 1 hour later, and an instant check would hold the spring-forward summary back an hour.
    private static bool IsWeekElapsed(DateOnly today, DateTime? lastAtUtc, TimeZoneInfo timeZone)
        => lastAtUtc is null
            || DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(lastAtUtc.Value, timeZone)) <= today.AddDays(-7);
}
