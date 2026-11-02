namespace Investments.Application.Commands.RecordContribution;

using Investments.Application.Common;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordContributionCommandValidator : ICommandValidator<RecordContributionCommand>
{
    public IEnumerable<ValidationError> Validate(RecordContributionCommand command)
    {
        if (!EnumValue.TryParse<ContributionDirection>(command.Direction, out _))
            yield return new ValidationError(nameof(command.Direction), $"Direction must be one of: {EnumValue.Names<ContributionDirection>()}.");

        if (!Money.TryParse(command.Amount, out var amount) || amount.MinorUnits == 0)
            yield return new ValidationError(
                nameof(command.Amount), "Amount must be a decimal such as 500.00: greater than zero, at most two decimals, at most 9999999999.99.");

        if (command.Note?.Trim().Length > Contribution.MaxNoteLength)
            yield return new ValidationError(nameof(command.Note), $"Note must be at most {Contribution.MaxNoteLength} characters.");
    }
}
