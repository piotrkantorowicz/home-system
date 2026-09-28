namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>One outbox row's serialised event, shown to an admin on demand.</summary>
/// <param name="Id">Primary key of the outbox row.</param>
/// <param name="EventType">Assembly-qualified name of the event type.</param>
/// <param name="Payload">The event serialised as JSON.</param>
public sealed record OutboxPayload(Guid Id, string EventType, string Payload);
