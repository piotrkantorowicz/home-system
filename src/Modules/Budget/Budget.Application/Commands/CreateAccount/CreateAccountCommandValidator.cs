namespace Budget.Application.Commands.CreateAccount;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class CreateAccountCommandValidator : ICommandValidator<CreateAccountCommand>
{
    public IEnumerable<ValidationError> Validate(CreateAccountCommand command)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > BudgetAccount.MaxNameLength)
            yield return new ValidationError(nameof(command.Name), $"Name must be 1–{BudgetAccount.MaxNameLength} characters.");

        if (!TryParseVisibility(command.Visibility, out var visibility))
            yield return new ValidationError(nameof(command.Visibility), "Visibility must be Household or Personal.");
        else if (visibility == AccountVisibility.Household && command.OwnerPersonId is not null)
            yield return new ValidationError(nameof(command.OwnerPersonId), "A household envelope has no owner.");
    }

    internal static bool TryParseVisibility(string? value, out AccountVisibility visibility)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out visibility)
           && Enum.IsDefined(visibility)
           && value!.Trim().All(char.IsLetter); // rejects numeric enum values such as "1"
}
