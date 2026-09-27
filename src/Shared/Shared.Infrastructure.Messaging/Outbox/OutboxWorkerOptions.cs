namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Tuning for every module's <see cref="OutboxWorker{TDbContext}"/>, bound from the
/// <c>Messaging:Outbox</c> configuration section. One set of values applies to all workers.
/// </summary>
public sealed class OutboxWorkerOptions
{
    /// <summary>Configuration section the options bind from.</summary>
    public const string SectionName = "Messaging:Outbox";

    /// <summary>Maximum number of pending messages a worker dispatches per tick.</summary>
    public int BatchSize { get; init; } = 50;

    /// <summary>Delay between ticks, in milliseconds; bounds delivery latency.</summary>
    public int PollIntervalMs { get; init; } = 1000;

    /// <summary>
    /// Failed dispatch attempts after which a message is dead-lettered: the worker stops picking it
    /// up until an admin retries it through <see cref="IOutboxDeadLetterStore.RetryAsync"/>.
    /// </summary>
    public int MaxAttempts { get; init; } = 10;

    /// <summary>
    /// Delay before the first retry, in milliseconds; doubles after each further failure up to
    /// <see cref="RetryMaxDelayMs"/>. With the defaults a message dead-letters after ~8.5 minutes.
    /// </summary>
    public int RetryBaseDelayMs { get; init; } = 1000;

    /// <summary>Upper bound for the retry delay, in milliseconds.</summary>
    public int RetryMaxDelayMs { get; init; } = 300_000;
}
