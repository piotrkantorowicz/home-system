namespace DietPlanner.Application.Workers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

internal sealed class WaterReminderJob(
    IWaterReminderCandidateQueries queries,
    IWaterReminderStateRepository stateRepo,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork,
    IHouseholdQueryService persons) : IDietReminderJob
{
    public string Name => "WaterReminderJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<WaterReminderCandidate> candidates = await queries.GetCandidatesAsync(nowUtc, ct);

        var publishedAny = false;

        foreach (var c in candidates)
        {
            var nowLocal = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, c.TimeZone));
            if (!IsInWindow(nowLocal, c.WaterWindowStart, c.WaterWindowEnd))
                continue;

            if (!IsIntervalPassed(nowUtc, c.LastWaterReminderAt, c.WaterReminderIntervalMinutes))
                continue;

            string? authSubject = await persons.GetAuthSubjectForPersonAsync(c.PersonId, ct);
            if (authSubject is null) continue;

            var existing = await stateRepo.GetByPersonIdAsync(c.PersonId, ct);
            if (existing is null)
                await stateRepo.AddAsync(WaterReminderState.Create(c.PersonId, nowUtc), ct);
            else
                existing.UpdateLastReminderAt(nowUtc);

            await bus.PublishAsync(new WaterReminderDueIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: nowUtc,
                UserId: authSubject,
                Locale: c.Locale), ct);

            publishedAny = true;
        }

        if (publishedAny)
            await unitOfWork.CommitAsync(ct);
    }

    private static bool IsInWindow(TimeOnly now, TimeOnly start, TimeOnly end)
        => now >= start && now < end;

    private static bool IsIntervalPassed(DateTime nowUtc, DateTime? lastAt, int intervalMinutes)
        => lastAt is null || lastAt.Value.AddMinutes(intervalMinutes) <= nowUtc;
}
