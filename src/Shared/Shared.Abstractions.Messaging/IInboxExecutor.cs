namespace Shared.Abstractions.Messaging;

/// <summary>
/// Idempotent execution boundary for an integration-event handler in a consuming module.
/// Implementations open a transaction on the consumer's storage, check the inbox for the
/// given event identifier, invoke the handler only on the first delivery, persist the inbox
/// marker, and commit. Implementations are owned by concrete persistence packages (EF, Dapper) —
/// never depend on a specific implementation from outside those packages.
/// </summary>
public interface IInboxExecutor
{
    /// <summary>
    /// Runs <paramref name="handlerInvocation"/> at most once for <paramref name="eventId"/>. A
    /// redelivery of an already-consumed event is a silent no-op; if the handler throws, nothing is
    /// recorded and the outbox worker retries on its next tick.
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
