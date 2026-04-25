namespace Shared.Abstractions.Cqrs;

public sealed class CommandValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public CommandValidationException(string commandName, IReadOnlyList<ValidationError> errors)
        : base($"Validation failed for command '{commandName}'.")
        => Errors = errors;
}
