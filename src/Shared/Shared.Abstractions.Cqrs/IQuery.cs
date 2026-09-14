namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Marker for a read request. Queries are immutable records dispatched through
/// <see cref="IQueryDispatcher"/> to a single <see cref="IQueryHandler{TQuery,TResult}"/> that
/// projects straight from storage into DTOs — no aggregates, no repository, no transaction.
/// </summary>
/// <typeparam name="TResult">The DTO (or paged list of DTOs) the query returns; nullable when not-found is expected.</typeparam>
public interface IQuery<out TResult> { }
