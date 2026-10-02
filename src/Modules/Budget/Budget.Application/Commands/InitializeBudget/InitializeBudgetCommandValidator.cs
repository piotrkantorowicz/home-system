namespace Budget.Application.Commands.InitializeBudget;

using Shared.Abstractions.Cqrs;

internal sealed class InitializeBudgetCommandValidator : ICommandValidator<InitializeBudgetCommand>
{
    public IEnumerable<ValidationError> Validate(InitializeBudgetCommand command)
    {
        if (command.Currency is not null && !InitializeBudgetCommandHandler.TryParseCurrency(command.Currency, out _))
            yield return new ValidationError(nameof(command.Currency), "Currency must be PLN, EUR or USD.");
    }
}
