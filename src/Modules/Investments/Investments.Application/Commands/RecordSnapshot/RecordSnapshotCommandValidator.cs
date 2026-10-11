namespace Investments.Application.Commands.RecordSnapshot;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordSnapshotCommandValidator : ICommandValidator<RecordSnapshotCommand>
{
    public IEnumerable<ValidationError> Validate(RecordSnapshotCommand command)
    {
        if (!Money.TryParse(command.Amount, out _))
            yield return new ValidationError(
                nameof(command.Amount), "Amount must be a decimal such as 12450.10: not negative, at most two decimals, at most 9999999999.99.");

        if (command.Note?.Trim().Length > BalanceSnapshot.MaxNoteLength)
            yield return new ValidationError(nameof(command.Note), $"Note must be at most {BalanceSnapshot.MaxNoteLength} characters.");
    }
}
