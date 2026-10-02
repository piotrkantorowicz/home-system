namespace Budget.Application.Commands.ClearMonthlyLimit;

using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class ClearMonthlyLimitCommandValidator : ICommandValidator<ClearMonthlyLimitCommand>
{
    public IEnumerable<ValidationError> Validate(ClearMonthlyLimitCommand command)
    {
        if (!BudgetMonth.TryParse(command.Month, out _))
            yield return new ValidationError(nameof(command.Month), "Month must be YYYY-MM.");

        if (command.ExpectedRevision < 1)
            yield return new ValidationError(nameof(command.ExpectedRevision), "The expected revision is required.");
    }
}
