namespace Shared.Abstractions.CQRS;

public interface ICommandValidator<in TCommand>
{
    IEnumerable<ValidationError> Validate(TCommand command);
}

public sealed record ValidationError(string PropertyName, string ErrorMessage);
