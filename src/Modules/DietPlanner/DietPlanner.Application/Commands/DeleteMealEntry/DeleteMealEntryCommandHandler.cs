namespace DietPlanner.Application.Commands.DeleteMealEntry;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteMealEntryCommandHandler(
    IMealEntryRepository repository,
    IUnitOfWork unitOfWork,
    HouseholdRosterProvider households) : ICommandHandler<DeleteMealEntryCommand>
{
    public async Task HandleAsync(DeleteMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        roster.Demand(entry.PersonId, roster.CanPlanFor(entry.PersonId), "MealEntry", command.Id);

        repository.Delete(entry);
        await unitOfWork.CommitAsync(ct);
    }
}
