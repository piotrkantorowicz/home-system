namespace DietPlanner.Application.Commands.CreateRecipe;

using Shared.Abstractions.Cqrs;

internal sealed class CreateRecipeCommandValidator : ICommandValidator<CreateRecipeCommand>
{
    public IEnumerable<ValidationError> Validate(CreateRecipeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            yield return new ValidationError(nameof(command.Name), "Name is required.");
        else if (command.Name.Length > 200)
            yield return new ValidationError(nameof(command.Name), "Name must not exceed 200 characters.");

        if (command.Servings <= 0)
            yield return new ValidationError(nameof(command.Servings), "Servings must be positive.");

        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.Ingredients.Count == 0)
            yield return new ValidationError(nameof(command.Ingredients), "Recipe must have at least one ingredient.");
    }
}
