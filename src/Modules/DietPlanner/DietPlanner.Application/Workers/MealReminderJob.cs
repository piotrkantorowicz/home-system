namespace DietPlanner.Application.Workers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

internal sealed class MealReminderJob(
    IMealReminderCandidateQueries queries,
    ISentMealReminderRepository ledger,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork) : IDietReminderJob
{
    public string Name => "MealReminderJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<MealReminderCandidate> due = await queries.GetDueRemindersAsync(nowUtc, ct);
        IReadOnlyList<MealReminderCandidate> missed = await queries.GetMissedRemindersAsync(nowUtc, ct);

        var publishedAny = false;

        foreach (var c in due)
        {
            var id = MealEntryId.From(c.MealEntryId);
            if (await ledger.ExistsAsync(id, MealReminderKind.Reminder, ct)) continue;

            await bus.PublishAsync(new MealReminderDueIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: nowUtc,
                UserId: c.UserId,
                Locale: c.Locale,
                MealEntryId: c.MealEntryId,
                MealSlotName: c.MealSlotName,
                PlannedAt: c.PlannedAtUtc), ct);

            await ledger.AddAsync(SentMealReminder.Create(id, MealReminderKind.Reminder, nowUtc), ct);
            publishedAny = true;
        }

        foreach (var c in missed)
        {
            var id = MealEntryId.From(c.MealEntryId);
            if (await ledger.ExistsAsync(id, MealReminderKind.Missed, ct)) continue;

            await bus.PublishAsync(new MealMissedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: nowUtc,
                UserId: c.UserId,
                Locale: c.Locale,
                MealEntryId: c.MealEntryId,
                MealSlotName: c.MealSlotName,
                PlannedAt: c.PlannedAtUtc), ct);

            await ledger.AddAsync(SentMealReminder.Create(id, MealReminderKind.Missed, nowUtc), ct);
            publishedAny = true;
        }

        if (publishedAny)
            await unitOfWork.CommitAsync(ct);
    }
}
