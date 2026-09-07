namespace Household.Application.Commands.SyncCurrentPerson;

using Shared.Abstractions.Cqrs;

internal sealed class SyncCurrentPersonCommandValidator : ICommandValidator<SyncCurrentPersonCommand>
{
    public IEnumerable<ValidationError> Validate(SyncCurrentPersonCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.AuthSubject))
            yield return new ValidationError(nameof(command.AuthSubject), "AuthSubject must not be empty.");

        if (string.IsNullOrWhiteSpace(command.DisplayName))
            yield return new ValidationError(nameof(command.DisplayName), "DisplayName must not be empty.");
    }
}
