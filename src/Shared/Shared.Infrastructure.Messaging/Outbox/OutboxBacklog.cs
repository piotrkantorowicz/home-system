namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>Undelivered outbox messages of one module.</summary>
/// <param name="DeadLettered">Messages that used up their attempts; only an admin requeue moves them.</param>
/// <param name="Retrying">Messages that failed at least once and are still being retried.</param>
public sealed record OutboxBacklog(int DeadLettered, int Retrying);
