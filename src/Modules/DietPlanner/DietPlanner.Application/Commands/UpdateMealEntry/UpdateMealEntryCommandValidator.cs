namespace DietPlanner.Application.Commands.UpdateMealEntry;

using Shared.Abstractions.Cqrs;

internal sealed class UpdateMealEntryCommandValidator : ICommandValidator<UpdateMealEntryCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateMealEntryCommand command)
    {
        if (command.Id == Guid.Empty)
            yield return new ValidationError(nameof(command.Id), "Id is required.");

        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

        if (command.MealSlotId == Guid.Empty)
            yield return new ValidationError(nameof(command.MealSlotId), "MealSlotId is required.");

        if (command.RecipeId == Guid.Empty)
            yield return new ValidationError(nameof(command.RecipeId), "RecipeId is required.");

        if (command.Servings <= 0)
            yield return new ValidationError(nameof(command.Servings), "Servings must be positive.");
    }
}
