namespace Shared.Abstractions.Messaging;

public interface IIntegrationEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
