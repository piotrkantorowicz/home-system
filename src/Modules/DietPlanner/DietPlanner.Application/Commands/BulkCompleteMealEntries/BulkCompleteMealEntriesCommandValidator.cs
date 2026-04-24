namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using Shared.Abstractions.CQRS;

internal sealed class BulkCompleteMealEntriesCommandValidator
    : ICommandValidator<BulkCompleteMealEntriesCommand>
{
    public IEnumerable<ValidationError> Validate(BulkCompleteMealEntriesCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.Date > DateOnly.FromDateTime(DateTime.UtcNow))
            yield return new ValidationError(
                nameof(command.Date), "Cannot bulk-complete meals for a future date.");
    }
}
