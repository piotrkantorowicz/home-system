namespace DietPlanner.Application.Commands.LogWaterIntake;

using Shared.Abstractions.Cqrs;

internal sealed class LogWaterIntakeCommandValidator : ICommandValidator<LogWaterIntakeCommand>
{
    public IEnumerable<ValidationError> Validate(LogWaterIntakeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.AmountMl is < 1 or > 5000)
            yield return new ValidationError(nameof(command.AmountMl), "AmountMl must be between 1 and 5000.");
    }
}
