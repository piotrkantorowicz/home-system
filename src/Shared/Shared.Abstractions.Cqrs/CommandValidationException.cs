namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Thrown by the validation dispatcher decorator when a command's <see cref="ICommandValidator{TCommand}"/>
/// reports at least one error, before the handler runs. The API boundary maps it to HTTP 400 and
/// groups <see cref="Errors"/> by property name in the problem details body.
/// </summary>
public sealed class CommandValidationException : Exception
{
    /// <summary>Every validation error found, in the order the validator yielded them.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>Creates the exception for a command that failed validation.</summary>
    /// <param name="commandName">The command type name, used in the exception message.</param>
    /// <param name="errors">The errors the validator produced; must not be empty.</param>
    public CommandValidationException(string commandName, IReadOnlyList<ValidationError> errors)
        : base($"Validation failed for command '{commandName}'.")
        => Errors = errors;
}
