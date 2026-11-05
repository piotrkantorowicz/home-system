namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>Undelivered outbox messages that still have attempts left (dead letters excluded).</summary>
/// <param name="Count">Number of such messages.</param>
/// <param name="OldestOccurredAt">Occurrence time (UTC) of the oldest, or <c>null</c> when there are none.</param>
public readonly record struct OutboxPending(int Count, DateTime? OldestOccurredAt);
