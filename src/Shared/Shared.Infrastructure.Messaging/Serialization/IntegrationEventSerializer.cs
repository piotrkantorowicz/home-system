namespace Shared.Infrastructure.Messaging.Serialization;

using System.Text.Json;
using Shared.Abstractions.Messaging;

/// <summary>
/// Converts integration events to and from the JSON stored in the outbox <c>payload</c> column.
/// </summary>
public interface IIntegrationEventSerializer
{
    /// <summary>Serialises the event using its runtime type so every property is written.</summary>
    /// <param name="integrationEvent">The event to serialise.</param>
    /// <returns>The JSON payload.</returns>
    string Serialize(IIntegrationEvent integrationEvent);

    /// <summary>Rebuilds an event from an outbox row.</summary>
    /// <param name="payload">The JSON written by <see cref="Serialize"/>.</param>
    /// <param name="eventType">The assembly-qualified type name stored alongside the payload.</param>
    /// <returns>The event, typed as its concrete record.</returns>
    IIntegrationEvent Deserialize(string payload, string eventType);
}

/// <summary>
/// <see cref="System.Text.Json"/>-based serializer. Because the event type is resolved by name from
/// untrusted storage, deserialisation only accepts types whose assembly-qualified name starts with
/// an allow-listed prefix (the <c>Shared.*</c> and <c>*.Contracts</c> namespaces by default).
/// </summary>
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

    /// <inheritdoc />
    public string Serialize(IIntegrationEvent integrationEvent)
        => JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The type is not in the allowlist, cannot be loaded, or the payload does not deserialise to it.
    /// </exception>
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
