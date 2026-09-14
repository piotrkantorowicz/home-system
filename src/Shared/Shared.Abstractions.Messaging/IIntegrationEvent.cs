namespace Shared.Abstractions.Messaging;

/// <summary>
/// A fact one module publishes for others to react to. Integration events live in the publishing
/// module's <c>Contracts</c> project as immutable records built from primitives only, are
/// serialised to JSON in the outbox and delivered at least once — consumers are made idempotent
/// by the inbox keyed on <see cref="EventId"/>.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique identity of this occurrence; the inbox uses it to drop redeliveries.</summary>
    Guid EventId { get; }

    /// <summary>When the fact happened, in UTC; the outbox dispatches in this order.</summary>
    DateTime OccurredAt { get; }
}
