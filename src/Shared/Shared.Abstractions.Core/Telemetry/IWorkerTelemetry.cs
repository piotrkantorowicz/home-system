namespace Shared.Abstractions.Core.Telemetry;

/// <summary>
/// Measures background worker ticks. Workers depend on this; the host registers the one implementation
/// (docs/rules/backend-observability.md).
/// </summary>
public interface IWorkerTelemetry
{
    /// <summary>
    /// Runs one tick inside a new root span named <c>&lt;worker&gt; tick</c> and records its outcome. The exception of
    /// a failed tick is rethrown; cancellation through <paramref name="ct"/> is recorded as <c>cancelled</c>, not
    /// <c>error</c>.
    /// </summary>
    /// <param name="worker">Low-cardinality worker name (the type name, plus the module for per-module workers).</param>
    /// <param name="tick">The tick body.</param>
    /// <param name="ct">The worker's stopping token, passed to <paramref name="tick"/>.</param>
    /// <returns>A task that completes with the tick.</returns>
    Task TrackTickAsync(string worker, Func<CancellationToken, Task> tick, CancellationToken ct);
}
