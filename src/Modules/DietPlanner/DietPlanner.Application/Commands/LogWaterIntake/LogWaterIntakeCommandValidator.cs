namespace DietPlanner.Application.Commands.LogWaterIntake;

using DietPlanner.Application.OperationReceipts;
using Shared.Abstractions.Cqrs;

internal sealed class LogWaterIntakeCommandValidator : ICommandValidator<LogWaterIntakeCommand>
{
    public IEnumerable<ValidationError> Validate(LogWaterIntakeCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

        if (command.AmountMl is < 1 or > 5000)
            yield return new ValidationError(nameof(command.AmountMl), "AmountMl must be between 1 and 5000.");

        if (command.OperationKey is not null && !OperationKeyInput.IsValid(command.OperationKey))
            yield return new ValidationError(nameof(command.OperationKey), OperationKeyInput.Error);
    }
}
