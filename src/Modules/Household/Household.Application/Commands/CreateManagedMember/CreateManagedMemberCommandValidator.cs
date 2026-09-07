using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.CreateManagedMember;

internal sealed class CreateManagedMemberCommandValidator : ICommandValidator<CreateManagedMemberCommand>
{
    public IEnumerable<ValidationError> Validate(CreateManagedMemberCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName))
            yield return new ValidationError(nameof(command.DisplayName), "A name is required.");

        if (command.Role == HouseholdRole.Owner)
            yield return new ValidationError(nameof(command.Role), "A managed member cannot be an owner.");
    }
}
