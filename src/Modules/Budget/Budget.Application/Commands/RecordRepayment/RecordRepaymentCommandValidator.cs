namespace Budget.Application.Commands.RecordRepayment;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordRepaymentCommandValidator : ICommandValidator<RecordRepaymentCommand>
{
    public IEnumerable<ValidationError> Validate(RecordRepaymentCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
            yield return new ValidationError(nameof(command.ClientRequestId), "A client request ID is required.");

        if (command.FromPersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.FromPersonId), "Choose who paid.");

        if (command.ToPersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.ToPersonId), "Choose who received the payment.");

        if (command.FromPersonId != Guid.Empty && command.FromPersonId == command.ToPersonId)
            yield return new ValidationError(nameof(command.ToPersonId), "Sender and recipient must be different people.");

        if (!Money.TryParsePositive(command.Amount, out _))
            yield return new ValidationError(nameof(command.Amount), "Amount must be a positive number with at most two decimals, up to 9999999999.99.");

        if (command.PaidOn.Year < 2000)
            yield return new ValidationError(nameof(command.PaidOn), "Enter the date of the payment.");

        if (command.Note?.Trim().Length > Settlement.MaxNoteLength)
            yield return new ValidationError(nameof(command.Note), $"A note can be at most {Settlement.MaxNoteLength} characters.");
    }
}
