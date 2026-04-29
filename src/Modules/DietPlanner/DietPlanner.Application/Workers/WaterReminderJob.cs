using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

namespace DietPlanner.Application.Workers;

internal sealed class WaterReminderJob(
    IWaterReminderCandidateQueries queries,
    IWaterReminderStateRepository stateRepo,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork) : IDietReminderJob
{
    public string Name => "WaterReminderJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<WaterReminderCandidate> candidates = await queries.GetCandidatesAsync(nowUtc, ct);

        var nowTime = TimeOnly.FromDateTime(nowUtc);
        var publishedAny = false;

        foreach (var c in candidates)
        {
            if (!IsInWindow(nowTime, c.WaterWindowStartUtc, c.WaterWindowEndUtc))
                continue;

            if (!IsIntervalPassed(nowUtc, c.LastWaterReminderAt, c.WaterReminderIntervalMinutes))
                continue;

            var existing = await stateRepo.GetByUserIdAsync(c.UserId, ct);
            if (existing is null)
                await stateRepo.AddAsync(WaterReminderState.Create(c.UserId, nowUtc), ct);
            else
                existing.UpdateLastReminderAt(nowUtc);

            await bus.PublishAsync(new WaterReminderDueIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: nowUtc,
                UserId: c.UserId,
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
