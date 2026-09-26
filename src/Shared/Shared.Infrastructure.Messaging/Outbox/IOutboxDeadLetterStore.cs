namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Core.Pagination;

/// <summary>
/// Admin view of one publishing module's dead-lettered outbox rows: undelivered messages whose
/// failed attempts reached <see cref="OutboxWorkerOptions.MaxAttempts"/>, which the
/// <see cref="OutboxWorker{TDbContext}"/> no longer picks up. Keyed by the module's
/// <c>DbContext</c> type, like <see cref="IOutboxStore"/>.
/// </summary>
public interface IOutboxDeadLetterStore
{
    /// <summary>Pages through dead-lettered messages, oldest first.</summary>
    /// <param name="maxAttempts">The attempt limit that marks a message dead.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<PagedList<OutboxDeadLetter>> ListAsync(int maxAttempts, int page, int pageSize, CancellationToken ct);

    /// <summary>Counts undelivered messages, split into dead-lettered and still retrying.</summary>
    /// <param name="maxAttempts">The attempt limit that marks a message dead.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task<OutboxBacklog> CountAsync(int maxAttempts, CancellationToken ct);

    /// <summary>
    /// Resets the attempt count of an undelivered message so the worker dispatches it again on its
    /// next tick. The last error is kept for diagnostics until the next attempt overwrites it.
    /// </summary>
    /// <param name="messageId">The outbox row to requeue.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns><see langword="false"/> when no undelivered row has that id.</returns>
    Task<bool> RequeueAsync(Guid messageId, CancellationToken ct);
}
