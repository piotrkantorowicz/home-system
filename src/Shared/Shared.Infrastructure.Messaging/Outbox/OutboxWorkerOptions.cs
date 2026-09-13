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
}
