namespace Shared.Abstractions.Messaging;

/// <summary>
/// Publishes an integration event to other modules. The registered implementation writes the event
/// to the publishing module's outbox inside the current unit of work, so the event is committed
/// atomically with the aggregate change and delivered later by the outbox worker — publishing is
/// never a direct call into another module.
/// </summary>
public interface IIntegrationEventBus
{
    /// <summary>Queues the event in the outbox of the module whose transaction is current.</summary>
    /// <typeparam name="TEvent">The concrete event record from a <c>Contracts</c> project.</typeparam>
    /// <param name="integrationEvent">The event to publish; its <see cref="IIntegrationEvent.EventId"/> must be unique.</param>
    /// <param name="ct">Propagates cancellation to the outbox write.</param>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
