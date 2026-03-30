namespace Shared.Infrastructure.CQRS.Decorators;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class TransactionCommandDispatcherDecorator<TDbContext> : ICommandDispatcher
    where TDbContext : DbContext
{
    private readonly ICommandDispatcher _inner;
    private readonly TDbContext _dbContext;

    public TransactionCommandDispatcherDecorator(
        ICommandDispatcher inner,
        TDbContext dbContext)
        => (_inner, _dbContext) = (inner, dbContext);

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        await _inner.SendAsync(command, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
        await tx.CommitAsync(ct);
        return result;
    }
}
