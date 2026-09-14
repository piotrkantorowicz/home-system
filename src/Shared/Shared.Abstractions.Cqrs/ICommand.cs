namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Marker for a write request that returns nothing. Commands are immutable records dispatched
/// through <see cref="ICommandDispatcher"/>, validated by an optional
/// <see cref="ICommandValidator{TCommand}"/> and executed inside one transaction by a single
/// <see cref="ICommandHandler{TCommand}"/>.
/// </summary>
public interface ICommand { }

/// <summary>
/// Marker for a write request that returns a value — typically the identifier of what it created.
/// Same dispatch, validation and transaction semantics as <see cref="ICommand"/>.
/// </summary>
/// <typeparam name="TResult">The value the handler returns to the caller.</typeparam>
public interface ICommand<out TResult> { }
