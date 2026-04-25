namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using Shared.Abstractions.Cqrs;

internal sealed class BulkCompleteMealEntriesCommandValidator
    : ICommandValidator<BulkCompleteMealEntriesCommand>
{
    public IEnumerable<ValidationError> Validate(BulkCompleteMealEntriesCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");
    }
}
