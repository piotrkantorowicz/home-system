namespace Budget.Application.Commands.UpdateExpense;

using Budget.Application.Commands.CreateExpense;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateExpenseCommandValidator : ICommandValidator<UpdateExpenseCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateExpenseCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
            yield return new ValidationError(nameof(command.ClientRequestId), "A client request ID is required.");

        if (command.ExpectedRevision < 1)
            yield return new ValidationError(nameof(command.ExpectedRevision), "The expected revision is required.");

        var reason = command.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > Expense.MaxReasonLength)
            yield return new ValidationError(nameof(command.Reason), $"A reason of 1–{Expense.MaxReasonLength} characters is required.");

        if (!Money.TryParsePositive(command.Amount, out _))
            yield return new ValidationError(nameof(command.Amount), "Amount must be a positive number with at most two decimals, up to 9999999999.99.");

        if (!CreateExpenseCommandValidator.TryParseCategory(command.Category, out _))
            yield return new ValidationError(nameof(command.Category), "Unknown category.");

        if (command.FundingSource is not null && !CreateExpenseCommandValidator.TryParseFunding(command.FundingSource, out _))
            yield return new ValidationError(nameof(command.FundingSource), "Funding source must be Individual or HouseholdFunds.");

        if (command.ParticipantIds is { } ids && ids.Distinct().Count() != ids.Count)
            yield return new ValidationError(nameof(command.ParticipantIds), "A participant can only be listed once.");
    }
}
