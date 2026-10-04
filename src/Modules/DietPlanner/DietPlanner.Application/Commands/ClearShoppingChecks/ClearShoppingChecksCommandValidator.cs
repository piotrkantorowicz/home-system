namespace DietPlanner.Application.Commands.ClearShoppingChecks;

using Shared.Abstractions.Cqrs;

internal sealed class ClearShoppingChecksCommandValidator : ICommandValidator<ClearShoppingChecksCommand>
{
    public IEnumerable<ValidationError> Validate(ClearShoppingChecksCommand command)
    {
        if (command.From > command.To)
            yield return new ValidationError(nameof(command.From), "From must not be after To.");
    }
}
