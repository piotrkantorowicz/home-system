using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.RenameHousehold;

internal sealed class RenameHouseholdCommandValidator : ICommandValidator<RenameHouseholdCommand>
{
    public IEnumerable<ValidationError> Validate(RenameHouseholdCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            yield return new ValidationError(nameof(command.Name), "A household name is required.");
        else if (command.Name.Trim().Length > 120)
            yield return new ValidationError(nameof(command.Name), "A household name must be 120 characters or fewer.");
    }
}
