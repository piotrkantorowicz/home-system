namespace DietPlanner.Application.Commands.UpdateMealEntry;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateMealEntryCommandHandler(
    IMealEntryRepository repository,
    IMealScheduleConfigRepository scheduleRepository,
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    HouseholdRosterProvider households) : ICommandHandler<UpdateMealEntryCommand>
{
    public async Task HandleAsync(UpdateMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        roster.Demand(entry.PersonId, roster.CanPlanFor(entry.PersonId), "MealEntry", command.Id);

        var recipeId = RecipeId.From(command.RecipeId);
        if (recipeId != entry.RecipeId)
        {
            LibraryAccess library = await households.GetLibraryAccessAsync(command.AuthSubject, ct);
            library.DemandReadable(await recipeRepository.GetByIdAsync(recipeId, ct), command.RecipeId);
        }

        var slotId = MealSlotId.From(command.MealSlotId);
        var schedule = await scheduleRepository.GetByPersonIdAsync(entry.PersonId, ct)
            ?? throw new NotFoundException("MealScheduleConfig", entry.PersonId);

        if (schedule.Slots.All(s => s.Id != slotId))
            throw new NotFoundException("MealSlot", command.MealSlotId);

        entry.Update(command.Date, slotId, recipeId,
            command.Servings, command.Notes, command.MealTime, command.SequenceOrder);

        repository.Update(entry);
        await unitOfWork.CommitAsync(ct);
    }
}
