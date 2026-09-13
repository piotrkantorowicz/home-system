namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Entry point endpoints use to execute a command. The registered implementation is a decorator
/// chain (logging → validation → transaction → handler), so callers never resolve an
/// <see cref="ICommandHandler{TCommand}"/> directly.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>Validates and executes a command that returns nothing.</summary>
    /// <typeparam name="TCommand">The command type; its handler is resolved from DI.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="ct">Propagates cancellation to the handler.</param>
    /// <exception cref="CommandValidationException">A registered validator reported errors.</exception>
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand;

    /// <summary>Validates and executes a command and returns its result.</summary>
    /// <typeparam name="TCommand">The command type; its handler is resolved from DI.</typeparam>
    /// <typeparam name="TResult">The value the handler returns.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="ct">Propagates cancellation to the handler.</param>
    /// <returns>The handler's result.</returns>
    /// <exception cref="CommandValidationException">A registered validator reported errors.</exception>
    Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>;
}
