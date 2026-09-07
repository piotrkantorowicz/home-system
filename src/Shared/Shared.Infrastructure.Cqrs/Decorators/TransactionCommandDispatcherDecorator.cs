namespace Shared.Infrastructure.Cqrs.Decorators;

using System.Transactions;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Wraps command execution in an ambient <see cref="TransactionScope"/> so a handler that
/// flushes more than once (or a handler plus its domain-event handlers) commits atomically.
/// The scope is module-agnostic: whichever module's connection the handler opens enlists in
/// the ambient transaction. A single command touches one module's database (cross-module
/// writes are forbidden), so the scope never promotes to a distributed transaction.
/// </summary>
internal sealed class TransactionCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;

    public TransactionCommandDispatcherDecorator(ICommandDispatcher inner) => _inner = inner;

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        using var scope = CreateScope();
        await _inner.SendAsync(command, ct);
        scope.Complete();
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        using var scope = CreateScope();
        var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
        scope.Complete();
        return result;
    }

    private static TransactionScope CreateScope() => new(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        TransactionScopeAsyncFlowOption.Enabled);
}
