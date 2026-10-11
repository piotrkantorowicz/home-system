namespace Investments.Application.Commands.RecordCheckIn;

using Shared.Abstractions.Cqrs;

/// <summary>Shape only; per-entry rules are reported together, by wallet id, as one 422 by the handler.</summary>
internal sealed class RecordCheckInCommandValidator : ICommandValidator<RecordCheckInCommand>
{
    public const int MaxEntries = 100;

    public IEnumerable<ValidationError> Validate(RecordCheckInCommand command)
    {
        if (command.AsOf is null)
            yield return new ValidationError(nameof(command.AsOf), "Choose the date of the balances.");

        if (command.Entries is null || command.Entries.Count == 0)
            yield return new ValidationError(nameof(command.Entries), "Add at least one balance.");
        else if (command.Entries.Count > MaxEntries)
            yield return new ValidationError(nameof(command.Entries), $"A check-in takes at most {MaxEntries} balances.");
        else if (command.Entries.Select(e => e.WalletId).Distinct().Count() != command.Entries.Count)
            yield return new ValidationError(nameof(command.Entries), "Each wallet may appear only once.");
    }
}
