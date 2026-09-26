namespace DietPlanner.Application.Commands.CreateMealEntry;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateMealEntryCommandHandler(
    IMealEntryRepository repository,
    IMealScheduleConfigRepository scheduleRepository,
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    HouseholdRosterProvider households) : ICommandHandler<CreateMealEntryCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateMealEntryCommand command, CancellationToken ct = default)
    {
        var personId = command.ForPersonId ?? command.PersonId;
        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        if (!roster.CanPlanFor(personId))
            throw new ForbiddenException("Your household role does not allow planning meals for this person.");

        LibraryAccess library = await households.GetLibraryAccessAsync(command.AuthSubject, ct);
        library.DemandReadable(await recipeRepository.GetByIdAsync(RecipeId.From(command.RecipeId), ct), command.RecipeId);

        var now = clock.GetUtcNow().UtcDateTime;
        var slotId = MealSlotId.From(command.MealSlotId);
        var schedule = await scheduleRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new NotFoundException("MealScheduleConfig", personId);

        if (schedule.Slots.All(s => s.Id != slotId))
            throw new NotFoundException("MealSlot", command.MealSlotId);

        var id = MealEntryId.New();
        var entry = MealEntry.Create(id, personId, command.Date, slotId,
            RecipeId.From(command.RecipeId), command.Servings, command.Notes,
            command.MealTime, command.SequenceOrder, now);

        await repository.AddAsync(entry, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
