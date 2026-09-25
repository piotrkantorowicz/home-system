namespace DietPlanner.Application.Commands.CreateMealEntry;

using Shared.Abstractions.Cqrs;

internal sealed class CreateMealEntryCommandValidator : ICommandValidator<CreateMealEntryCommand>
{
    public IEnumerable<ValidationError> Validate(CreateMealEntryCommand command)
    {
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
