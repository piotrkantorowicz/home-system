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

    // Default allowlist used when registered via DI.
    private static readonly string[] DefaultAllowedPrefixes =
        ["Shared.", "DietPlanner.Contracts", "Notifications.Contracts"];

    private readonly IReadOnlyCollection<string> _allowedTypePrefixes;

    /// <summary>
    /// Parameterless constructor used by DI — applies the built-in prefix allowlist.
    /// </summary>
    public IntegrationEventSerializer()
        : this(DefaultAllowedPrefixes) { }

    /// <summary>
    /// Constructor for tests or host-level overrides that need a custom allowlist.
    /// Pass an explicit set of assembly-qualified-name prefixes to accept.
    /// </summary>
    public IntegrationEventSerializer(IReadOnlyCollection<string> allowedTypePrefixes)
        => _allowedTypePrefixes = allowedTypePrefixes;

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
