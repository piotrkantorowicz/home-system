namespace DietPlanner.Application.Commands.SetShoppingCheck;

using Shared.Abstractions.Cqrs;

internal sealed class SetShoppingCheckCommandValidator : ICommandValidator<SetShoppingCheckCommand>
{
    public IEnumerable<ValidationError> Validate(SetShoppingCheckCommand command)
    {
        if (command.ProductId == Guid.Empty)
            yield return new ValidationError(nameof(command.ProductId), "ProductId is required.");

        if (string.IsNullOrWhiteSpace(command.Unit) || command.Unit.Length > 50)
            yield return new ValidationError(nameof(command.Unit), "Unit is required and at most 50 characters.");

        if (command.From > command.To)
            yield return new ValidationError(nameof(command.From), "From must not be after To.");
    }
}
