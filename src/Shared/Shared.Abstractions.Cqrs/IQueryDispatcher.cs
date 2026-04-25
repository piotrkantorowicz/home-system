namespace Shared.Abstractions.Cqrs;

public interface IQueryDispatcher
{
    Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>;
}
