namespace DietPlanner.Application.Commands.UpdateProduct;

using Shared.Abstractions.CQRS;

internal sealed class UpdateProductCommandValidator : ICommandValidator<UpdateProductCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateProductCommand command)
    {
        if (command.Id == Guid.Empty)
            yield return new ValidationError(nameof(command.Id), "Id is required.");

        if (string.IsNullOrWhiteSpace(command.Name))
            yield return new ValidationError(nameof(command.Name), "Name is required.");
        else if (command.Name.Length > 200)
            yield return new ValidationError(nameof(command.Name), "Name must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(command.DefaultUnit))
            yield return new ValidationError(nameof(command.DefaultUnit), "DefaultUnit is required.");

        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");
    }
}
