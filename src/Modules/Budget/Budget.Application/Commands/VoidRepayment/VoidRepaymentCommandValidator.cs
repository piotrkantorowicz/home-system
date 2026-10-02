namespace Budget.Application.Commands.VoidRepayment;

using Budget.Domain.Aggregates;
using Shared.Abstractions.Cqrs;

internal sealed class VoidRepaymentCommandValidator : ICommandValidator<VoidRepaymentCommand>
{
    public IEnumerable<ValidationError> Validate(VoidRepaymentCommand command)
    {
        if (command.ExpectedRevision < 1)
            yield return new ValidationError(nameof(command.ExpectedRevision), "The expected revision is required.");

        var reason = command.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > Settlement.MaxReasonLength)
            yield return new ValidationError(nameof(command.Reason), $"A reason of 1–{Settlement.MaxReasonLength} characters is required.");
    }
}
