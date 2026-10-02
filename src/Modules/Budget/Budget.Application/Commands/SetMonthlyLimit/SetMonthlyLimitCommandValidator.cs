namespace Budget.Application.Commands.SetMonthlyLimit;

using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class SetMonthlyLimitCommandValidator : ICommandValidator<SetMonthlyLimitCommand>
{
    public IEnumerable<ValidationError> Validate(SetMonthlyLimitCommand command)
    {
        if (!BudgetMonth.TryParse(command.Month, out _))
            yield return new ValidationError(nameof(command.Month), "Month must be YYYY-MM.");

        if (!Money.TryParseNonNegative(command.Amount, out _))
            yield return new ValidationError(nameof(command.Amount), "Amount must be a number with at most two decimals, from 0 to 9999999999.99.");

        if (command.ExpectedRevision is < 1)
            yield return new ValidationError(nameof(command.ExpectedRevision), "The expected revision must be at least 1.");
    }
}
