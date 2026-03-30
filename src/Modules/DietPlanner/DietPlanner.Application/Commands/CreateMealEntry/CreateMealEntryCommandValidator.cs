namespace DietPlanner.Application.Commands.CreateMealEntry;

using DietPlanner.Domain.Constants;
using Shared.Abstractions.CQRS;

internal sealed class CreateMealEntryCommandValidator : ICommandValidator<CreateMealEntryCommand>
{
    public IEnumerable<ValidationError> Validate(CreateMealEntryCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.RecipeId == Guid.Empty)
            yield return new ValidationError(nameof(command.RecipeId), "RecipeId is required.");

        if (command.Servings <= 0)
            yield return new ValidationError(nameof(command.Servings), "Servings must be positive.");

        var validMealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack };
        if (!validMealTypes.Contains(command.MealType))
            yield return new ValidationError(nameof(command.MealType),
                $"MealType must be one of: {string.Join(", ", validMealTypes)}.");
    }
}
