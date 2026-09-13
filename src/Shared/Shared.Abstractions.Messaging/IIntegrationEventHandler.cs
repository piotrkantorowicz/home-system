namespace Shared.Abstractions.Messaging;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Consumes an integration event published by another module. The transport invokes the handler
/// through <see cref="IInboxExecutor"/>, which skips events that were already consumed and
/// committed, so the handler does not repeat that check itself. It can still be invoked more than
/// once for the same event (a failed handler or commit is retried, and concurrent deliveries can
/// race the check), so keep its writes inside the consuming module's transaction and any external
/// side effect idempotent. Throwing leaves the event unconsumed and the outbox worker retries it.
/// </summary>
/// <typeparam name="TEvent">The event record from the publishing module's <c>Contracts</c> project.</typeparam>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The 'EventHandler' suffix names the handler of a domain/integration event, not a System.EventHandler delegate; the name is a public contract used by every module.")]
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>Handles one delivery of the event.</summary>
    /// <param name="integrationEvent">The deserialised event.</param>
    /// <param name="ct">Propagates cancellation to every I/O call.</param>
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}
