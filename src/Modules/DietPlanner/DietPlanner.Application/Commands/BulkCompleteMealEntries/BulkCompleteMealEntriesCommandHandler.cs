namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class BulkCompleteMealEntriesCommandHandler(
    IMealEntryRepository repository,
    IUnitOfWork unitOfWork,
    HouseholdRosterProvider households) : ICommandHandler<BulkCompleteMealEntriesCommand, BulkCompleteResult>
{
    public async Task<BulkCompleteResult> HandleAsync(
        BulkCompleteMealEntriesCommand command, CancellationToken ct = default)
    {
        var personId = command.ForPersonId ?? command.PersonId;
        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        if (!roster.CanLogFor(personId))
            throw new ForbiddenException("You can only complete your own meals or those of a managed member.");

        var entries = await repository.GetByPersonAndDateRangeAsync(personId, command.Date, command.Date, ct);

        var completed = 0;
        foreach (var entry in entries.Where(e => e.Status == MealEntryStatus.Planned))
        {
            entry.MarkDone();
            repository.Update(entry);
            completed++;
        }

        if (completed > 0)
            await unitOfWork.CommitAsync(ct);

        return new BulkCompleteResult(completed);
    }
}
