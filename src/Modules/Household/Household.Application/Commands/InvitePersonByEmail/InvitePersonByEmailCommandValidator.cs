namespace Household.Application.Commands.InvitePersonByEmail;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class InvitePersonByEmailCommandValidator : ICommandValidator<InvitePersonByEmailCommand>
{
    public IEnumerable<ValidationError> Validate(InvitePersonByEmailCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || !command.Email.Contains('@', StringComparison.Ordinal))
            yield return new ValidationError(nameof(command.Email), "A valid email is required.");

        if (command.Role == HouseholdRole.Owner)
            yield return new ValidationError(nameof(command.Role), "An invitation cannot grant the owner role.");
    }
}
