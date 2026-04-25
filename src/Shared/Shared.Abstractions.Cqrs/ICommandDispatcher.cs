namespace Shared.Abstractions.Cqrs;

public interface ICommandDispatcher
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand;

    Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>;
}
