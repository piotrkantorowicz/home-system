namespace DietPlanner.Application.Commands.LogWeightEntry;

using Shared.Abstractions.Cqrs;

internal sealed class LogWeightEntryCommandValidator(TimeProvider clock) : ICommandValidator<LogWeightEntryCommand>
{
    public IEnumerable<ValidationError> Validate(LogWeightEntryCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

        if (command.WeightKg <= 0 || command.WeightKg > 999)
            yield return new ValidationError(nameof(command.WeightKg), "WeightKg must be between 0 and 999.");

        if (command.Date > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            yield return new ValidationError(nameof(command.Date), "Date cannot be in the future.");
    }
}
