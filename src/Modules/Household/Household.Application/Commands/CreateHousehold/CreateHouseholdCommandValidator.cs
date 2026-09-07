using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.CreateHousehold;

internal sealed class CreateHouseholdCommandValidator : ICommandValidator<CreateHouseholdCommand>
{
    public IEnumerable<ValidationError> Validate(CreateHouseholdCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestingAuthSubject))
            yield return new ValidationError(nameof(command.RequestingAuthSubject), "Missing subject.");

        if (string.IsNullOrWhiteSpace(command.Name))
            yield return new ValidationError(nameof(command.Name), "A household name is required.");
        else if (command.Name.Trim().Length > 120)
            yield return new ValidationError(nameof(command.Name), "A household name must be 120 characters or fewer.");
    }
}
