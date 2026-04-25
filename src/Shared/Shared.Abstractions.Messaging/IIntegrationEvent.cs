namespace Shared.Abstractions.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
