namespace Budget.Application.Commands.VoidExpense;

using Budget.Domain.Aggregates;
using Shared.Abstractions.Cqrs;

internal sealed class VoidExpenseCommandValidator : ICommandValidator<VoidExpenseCommand>
{
    public IEnumerable<ValidationError> Validate(VoidExpenseCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
            yield return new ValidationError(nameof(command.ClientRequestId), "A client request ID is required.");

        if (command.ExpectedRevision < 1)
            yield return new ValidationError(nameof(command.ExpectedRevision), "The expected revision is required.");

        var reason = command.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > Expense.MaxReasonLength)
            yield return new ValidationError(nameof(command.Reason), $"A reason of 1–{Expense.MaxReasonLength} characters is required.");
    }
}
