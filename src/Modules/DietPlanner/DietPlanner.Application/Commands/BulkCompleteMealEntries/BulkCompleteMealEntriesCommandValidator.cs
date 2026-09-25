namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using Shared.Abstractions.Cqrs;

internal sealed class BulkCompleteMealEntriesCommandValidator
    : ICommandValidator<BulkCompleteMealEntriesCommand>
{
    public IEnumerable<ValidationError> Validate(BulkCompleteMealEntriesCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");
    }
}
