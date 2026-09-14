namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// Commits every change tracked during the current request as one atomic write. Each module binds
/// it to its own storage (an EF <c>DbContext</c> or a Dapper transaction); a command handler calls
/// <see cref="CommitAsync"/> exactly once at the end of its work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes, including any outbox rows written alongside them.</summary>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task CommitAsync(CancellationToken ct = default);
}
