namespace Shared.Infrastructure.Messaging.Transport;

using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Delivers one outbox message to its consumers. The seam between the outbox worker and the wire:
/// v1 is <see cref="InProcessIntegrationEventTransport"/>; a broker transport implements the same
/// contract and is swapped in by host wiring only.
/// </summary>
public interface IIntegrationEventTransport
{
    /// <summary>
    /// Delivers the message. Must throw when delivery did not complete so the worker records the
    /// failure and retries; returning normally marks the message processed.
    /// </summary>
    /// <param name="message">The pending outbox row.</param>
    /// <param name="ct">Propagates cancellation to the delivery.</param>
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
