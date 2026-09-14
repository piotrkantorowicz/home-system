namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Shape validation for a command (required fields, ranges, formats), run by the validation
/// dispatcher decorator before the handler. One optional validator per command type; business
/// rules that need state stay in the aggregate.
/// </summary>
/// <typeparam name="TCommand">The command being validated.</typeparam>
public interface ICommandValidator<in TCommand>
{
    /// <summary>Returns every problem with the command; an empty sequence means it is valid.</summary>
    /// <param name="command">The command to inspect.</param>
    /// <returns>Zero or more errors, typically produced with <c>yield return</c>.</returns>
    IEnumerable<ValidationError> Validate(TCommand command);
}

/// <summary>One validation failure, attributed to the command property that caused it.</summary>
/// <param name="PropertyName">The command property (or indexed path such as <c>Slots[2].Name</c>) at fault.</param>
/// <param name="ErrorMessage">Why the value is rejected, phrased for the end user.</param>
public sealed record ValidationError(string PropertyName, string ErrorMessage);
