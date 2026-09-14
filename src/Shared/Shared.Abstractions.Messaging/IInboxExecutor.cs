namespace Shared.Abstractions.Messaging;

/// <summary>
/// Idempotency boundary for an integration-event handler in a consuming module. Implementations
/// open a transaction on the consumer's storage, skip the event when a committed inbox marker for
/// its identifier already exists, otherwise invoke the handler, insert the marker and commit. The
/// marker is written <em>after</em> the handler, so an event whose handler or commit failed — or two
/// deliveries racing past the existence check — can invoke the handler again: the guarantee is
/// at-least-once invocation with at most one <em>committed</em> consumption, not exactly-once.
/// Implementations are owned by concrete persistence packages (EF, Dapper) — never depend on a
/// specific implementation from outside those packages.
/// </summary>
public interface IInboxExecutor
{
    /// <summary>
    /// Runs <paramref name="handlerInvocation"/> unless a committed inbox marker for
    /// <paramref name="eventId"/> already exists, in which case the call is a silent no-op. If the
    /// handler throws or the commit fails, no marker is written and the outbox worker retries on its
    /// next tick — so the handler may run more than once for the same event; keep its own writes
    /// inside the consuming module's transaction and any external side effect idempotent.
    /// </summary>
    /// <param name="eventId">The <see cref="IIntegrationEvent.EventId"/> used as the idempotency key.</param>
    /// <param name="eventType">The assembly-qualified event type name, stored with the inbox marker for diagnostics.</param>
    /// <param name="handlerInvocation">Invokes the consuming module's handler; receives the token to pass on.</param>
    /// <param name="ct">Propagates cancellation to the storage calls and the handler.</param>
    Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default);
}
