namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Entry point endpoints use to execute a query. The registered implementation wraps the handler in
/// the logging decorator only — queries are not validated or transactional.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>Executes a query and returns its projection.</summary>
    /// <typeparam name="TQuery">The query type; its handler is resolved from DI.</typeparam>
    /// <typeparam name="TResult">The DTO shape the handler returns.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="ct">Propagates cancellation to the handler.</param>
    /// <returns>The projected result; <see langword="null"/> only when <typeparamref name="TResult"/> is nullable and nothing matched.</returns>
    Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>;
}
