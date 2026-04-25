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

    private readonly IReadOnlyCollection<string> _allowedTypePrefixes;

    /// <summary>
    /// Creates a serializer that will only deserialize types whose assembly-qualified name
    /// starts with one of the allowed prefixes. Defaults to the project's Shared.* and
    /// module Contracts namespaces. Pass an explicit list to override (e.g. in tests).
    /// </summary>
    public IntegrationEventSerializer(IEnumerable<string>? allowedTypePrefixes = null)
        => _allowedTypePrefixes = (allowedTypePrefixes
            ?? new[] { "Shared.", "DietPlanner.Contracts", "Notifications.Contracts" }).ToArray();

    public string Serialize(IIntegrationEvent @event)
        => JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public IIntegrationEvent Deserialize(string payload, string eventType)
    {
        if (!_allowedTypePrefixes.Any(prefix => eventType.StartsWith(prefix, StringComparison.Ordinal)))
            throw new InvalidOperationException(
                $"Refusing to deserialize event type '{eventType}': not in allowlist.");

        var type = Type.GetType(eventType, throwOnError: true)
            ?? throw new InvalidOperationException($"Unknown event type: {eventType}");

        var result = JsonSerializer.Deserialize(payload, type, Options)
            ?? throw new InvalidOperationException($"Failed to deserialize event of type {eventType}");

        return (IIntegrationEvent)result;
    }
}
