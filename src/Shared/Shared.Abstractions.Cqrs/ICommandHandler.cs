namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Executes one <see cref="ICommand"/>. Handlers are <c>internal sealed</c>, registered by assembly
/// scan, own exactly one <c>IUnitOfWork.CommitAsync</c> call and are only ever invoked through
/// <see cref="ICommandDispatcher"/>.
/// </summary>
/// <typeparam name="TCommand">The command this handler executes.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Executes the command.</summary>
    /// <param name="command">The validated command.</param>
    /// <param name="ct">Propagates cancellation to every I/O call.</param>
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>
/// Executes one <see cref="ICommand{TResult}"/> and returns its result. Same rules as
/// <see cref="ICommandHandler{TCommand}"/>.
/// </summary>
/// <typeparam name="TCommand">The command this handler executes.</typeparam>
/// <typeparam name="TResult">The value returned to the caller, e.g. a new identifier.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Executes the command and returns its result.</summary>
    /// <param name="command">The validated command.</param>
    /// <param name="ct">Propagates cancellation to every I/O call.</param>
    /// <returns>The command's result.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
