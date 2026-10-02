namespace Budget.Application.Commands.RenameAccount;

using Budget.Domain.Aggregates;
using Shared.Abstractions.Cqrs;

internal sealed class RenameAccountCommandValidator : ICommandValidator<RenameAccountCommand>
{
    public IEnumerable<ValidationError> Validate(RenameAccountCommand command)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > BudgetAccount.MaxNameLength)
            yield return new ValidationError(nameof(command.Name), $"Name must be 1–{BudgetAccount.MaxNameLength} characters.");
    }
}
