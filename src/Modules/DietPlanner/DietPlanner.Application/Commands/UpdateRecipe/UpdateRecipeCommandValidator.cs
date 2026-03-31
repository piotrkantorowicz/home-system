namespace DietPlanner.Application.Commands.UpdateRecipe;

using Shared.Abstractions.CQRS;

internal sealed class UpdateRecipeCommandValidator : ICommandValidator<UpdateRecipeCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateRecipeCommand command)
    {
        if (command.Id == Guid.Empty)
            yield return new ValidationError(nameof(command.Id), "Id is required.");

        if (string.IsNullOrWhiteSpace(command.Name))
            yield return new ValidationError(nameof(command.Name), "Name is required.");
        else if (command.Name.Length > 200)
            yield return new ValidationError(nameof(command.Name), "Name must not exceed 200 characters.");

        if (command.Servings <= 0)
            yield return new ValidationError(nameof(command.Servings), "Servings must be positive.");

        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");
    }
}
