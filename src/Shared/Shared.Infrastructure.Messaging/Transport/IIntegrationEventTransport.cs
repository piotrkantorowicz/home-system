namespace Shared.Infrastructure.Messaging.Transport;

using Shared.Infrastructure.Messaging.Outbox;

public interface IIntegrationEventTransport
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
