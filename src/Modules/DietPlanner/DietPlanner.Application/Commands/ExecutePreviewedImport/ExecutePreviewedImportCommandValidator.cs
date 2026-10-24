namespace DietPlanner.Application.Commands.ExecutePreviewedImport;

using DietPlanner.Application.OperationReceipts;
using Shared.Abstractions.Cqrs;

internal sealed class ExecutePreviewedImportCommandValidator : ICommandValidator<ExecutePreviewedImportCommand>
{
    public IEnumerable<ValidationError> Validate(ExecutePreviewedImportCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.OperationKey is null || !OperationKeyInput.IsValid(command.OperationKey))
            yield return new ValidationError(nameof(command.OperationKey), OperationKeyInput.Error);
    }
}
