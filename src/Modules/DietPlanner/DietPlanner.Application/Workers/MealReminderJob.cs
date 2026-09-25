namespace DietPlanner.Application.Workers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

internal sealed class MealReminderJob(
    IMealReminderCandidateQueries queries,
    ISentMealReminderRepository ledger,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork,
    IHouseholdQueryService persons) : IDietReminderJob
{
    public string Name => "MealReminderJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<MealReminderCandidate> due = await queries.GetDueRemindersAsync(nowUtc, ct);
        IReadOnlyList<MealReminderCandidate> missed = await queries.GetMissedRemindersAsync(nowUtc, ct);

        var publishedAny = false;

        foreach (var c in due)
        {
            string? authSubject = await persons.GetAuthSubjectForPersonAsync(c.PersonId, ct);
            if (authSubject is null) continue;
            var id = MealEntryId.From(c.MealEntryId);
            if (await ledger.ExistsAsync(id, MealReminderKind.Reminder, ct)) continue;

            await bus.PublishAsync(new MealReminderDueIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: nowUtc,
                UserId: authSubject,
                Locale: c.Locale,
                MealEntryId: c.MealEntryId,
                MealSlotName: c.MealSlotName,
                PlannedAt: c.PlannedAtUtc), ct);

            await ledger.AddAsync(SentMealReminder.Create(id, MealReminderKind.Reminder, nowUtc), ct);
            publishedAny = true;
        }

        foreach (var c in missed)
        {
            string? authSubject = await persons.GetAuthSubjectForPersonAsync(c.PersonId, ct);
            if (authSubject is null) continue;
            var id = MealEntryId.From(c.MealEntryId);
            if (await ledger.ExistsAsync(id, MealReminderKind.Missed, ct)) continue;

            await bus.PublishAsync(new MealMissedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: nowUtc,
                UserId: authSubject,
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
