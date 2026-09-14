namespace Shared.Abstractions.Cqrs;

/// <summary>
/// Executes one <see cref="IQuery{TResult}"/>. Handlers are <c>internal sealed</c>, read through the
/// module's read-side storage (EF <c>AsNoTracking()</c> + <c>Select()</c>, or Dapper) and return DTOs
/// only — never domain objects.
/// </summary>
/// <typeparam name="TQuery">The query this handler executes.</typeparam>
/// <typeparam name="TResult">The DTO shape returned to the caller.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Executes the query.</summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="ct">Propagates cancellation to every I/O call.</param>
    /// <returns>The projected result.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
