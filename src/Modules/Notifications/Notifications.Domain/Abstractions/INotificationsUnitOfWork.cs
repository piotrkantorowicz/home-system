namespace Notifications.Domain.Abstractions;

/// <summary>
/// Module-scoped unit of work over the Dapper connection and transaction handlers write through.
/// Kept separate from the global <c>IUnitOfWork</c>, which the first EF module owns.
/// </summary>
public interface INotificationsUnitOfWork
{
    /// <summary>Commits the transaction opened by the first write; a no-op when nothing was written.</summary>
    /// <param name="ct">Propagates cancellation to the commit.</param>
    Task CommitAsync(CancellationToken ct = default);
}
