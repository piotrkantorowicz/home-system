namespace DietPlanner.Application.Commands.CreateProduct;

using Shared.Abstractions.CQRS;

internal sealed class CreateProductCommandValidator : ICommandValidator<CreateProductCommand>
{
    public IEnumerable<ValidationError> Validate(CreateProductCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            yield return new ValidationError(nameof(command.Name), "Name is required.");
        else if (command.Name.Length > 200)
            yield return new ValidationError(nameof(command.Name), "Name must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(command.DefaultUnit))
            yield return new ValidationError(nameof(command.DefaultUnit), "DefaultUnit is required.");

        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.Calories is < 0)
            yield return new ValidationError(nameof(command.Calories), "Calories must be non-negative.");

        if (command.Protein is < 0)
            yield return new ValidationError(nameof(command.Protein), "Protein must be non-negative.");

        if (command.Carbs is < 0)
            yield return new ValidationError(nameof(command.Carbs), "Carbs must be non-negative.");

        if (command.Fat is < 0)
            yield return new ValidationError(nameof(command.Fat), "Fat must be non-negative.");

        if (command.Fiber is < 0)
            yield return new ValidationError(nameof(command.Fiber), "Fiber must be non-negative.");
    }
}
