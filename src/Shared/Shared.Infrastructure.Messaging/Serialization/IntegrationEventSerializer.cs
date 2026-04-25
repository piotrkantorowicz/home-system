namespace Shared.Infrastructure.Messaging.Serialization;

using System.Text.Json;
using Shared.Abstractions.Messaging;

public interface IIntegrationEventSerializer
{
    string Serialize(IIntegrationEvent @event);
    IIntegrationEvent Deserialize(string payload, string eventType);
}

public sealed class IntegrationEventSerializer : IIntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(IIntegrationEvent @event)
        => JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public IIntegrationEvent Deserialize(string payload, string eventType)
    {
        var type = Type.GetType(eventType, throwOnError: true)
            ?? throw new InvalidOperationException($"Unknown event type: {eventType}");

        var result = JsonSerializer.Deserialize(payload, type, Options)
            ?? throw new InvalidOperationException($"Failed to deserialize event of type {eventType}");

        return (IIntegrationEvent)result;
    }
}
