namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Core.Pagination;

/// <summary>
/// Admin view of one publishing module's dead-lettered outbox rows: undelivered messages whose
/// failed attempts reached <see cref="OutboxWorkerOptions.MaxAttempts"/> and that were not retried yet, which the
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
    /// Retries an undelivered message as a new outbox row (same event id and payload, linked back via
    /// <c>retry_of</c>) that the worker dispatches on its next tick. The original row is marked retried
    /// and kept, with its attempts and last error, as history; it no longer counts as dead-lettered.
    /// </summary>
    /// <param name="messageId">The outbox row to retry.</param>
    /// <param name="now">Current time, UTC; recorded as the original's <c>retried_at</c>.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns><see langword="false"/> when no undelivered, not yet retried row has that id.</returns>
    Task<bool> RetryAsync(Guid messageId, DateTime now, CancellationToken ct);

    /// <summary>
    /// Retries up to <paramref name="limit"/> dead-lettered messages, oldest first, each exactly like
    /// <see cref="RetryAsync"/>, in one save.
    /// </summary>
    /// <param name="maxAttempts">The attempt limit that marks a message dead.</param>
    /// <param name="now">Current time, UTC; recorded as each original's <c>retried_at</c>.</param>
    /// <param name="limit">Most messages retried by one call.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>How many messages were retried.</returns>
    Task<int> RetryAllAsync(int maxAttempts, DateTime now, int limit, CancellationToken ct);
}
