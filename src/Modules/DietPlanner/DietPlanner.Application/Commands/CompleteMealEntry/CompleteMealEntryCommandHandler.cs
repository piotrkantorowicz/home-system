namespace DietPlanner.Application.Commands.CompleteMealEntry;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CompleteMealEntryCommandHandler(
    IMealEntryRepository repository,
    IUnitOfWork unitOfWork,
    HouseholdRosterProvider households) : ICommandHandler<CompleteMealEntryCommand>
{
    public async Task HandleAsync(CompleteMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        roster.Demand(entry.PersonId, roster.CanLogFor(entry.PersonId), "MealEntry", command.Id);

        entry.MarkDone();
        repository.Update(entry);
        await unitOfWork.CommitAsync(ct);
    }
}
