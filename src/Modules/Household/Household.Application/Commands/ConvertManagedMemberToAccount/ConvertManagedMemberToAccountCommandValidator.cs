namespace Household.Application.Commands.ConvertManagedMemberToAccount;

using Shared.Abstractions.Cqrs;

internal sealed class ConvertManagedMemberToAccountCommandValidator
    : ICommandValidator<ConvertManagedMemberToAccountCommand>
{
    public IEnumerable<ValidationError> Validate(ConvertManagedMemberToAccountCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestingAuthSubject))
            yield return new ValidationError(nameof(command.RequestingAuthSubject), "Missing subject.");

        if (command.HouseholdId == Guid.Empty)
            yield return new ValidationError(nameof(command.HouseholdId), "HouseholdId must not be empty.");

        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId must not be empty.");

        if (string.IsNullOrWhiteSpace(command.Email))
            yield return new ValidationError(nameof(command.Email), "An email address is required.");
    }
}
