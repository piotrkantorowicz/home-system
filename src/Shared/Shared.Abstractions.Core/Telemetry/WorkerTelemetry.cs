namespace Shared.Abstractions.Core.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Measures background worker ticks (<c>HomeSystem.Workers</c> source and meter): one root span per tick, so the
/// database calls a tick makes have a parent, plus a tick counter and a duration histogram tagged by worker and
/// outcome. Singleton, created through <see cref="IMeterFactory"/>. Conventions: docs/rules/backend-observability.md.
/// </summary>
public sealed class WorkerTelemetry
{
    /// <summary>Name of the <see cref="ActivitySource"/> and the <see cref="Meter"/>; the host subscribes to <c>HomeSystem.*</c>.</summary>
    public const string Name = "HomeSystem.Workers";

    /// <summary>Tag carrying the worker name.</summary>
    public const string WorkerTag = "homesystem.worker";

    /// <summary>Tag carrying the tick outcome.</summary>
    public const string OutcomeTag = "outcome";

    /// <summary>The tick completed.</summary>
    public const string Success = "success";

    /// <summary>The tick threw.</summary>
    public const string Error = "error";

    /// <summary>The tick stopped because the host is shutting down.</summary>
    public const string Cancelled = "cancelled";

    private static readonly ActivitySource Source = new(Name);

    private readonly Counter<long> _ticks;
    private readonly Histogram<double> _duration;

    /// <summary>Creates the meter and its instruments.</summary>
    /// <param name="meters">Factory that owns the meter's lifetime.</param>
    public WorkerTelemetry(IMeterFactory meters)
    {
        ArgumentNullException.ThrowIfNull(meters);

        var meter = meters.Create(Name);
        _ticks = meter.CreateCounter<long>("homesystem.worker.ticks", unit: "{tick}");
        _duration = meter.CreateHistogram<double>("homesystem.worker.tick.duration", unit: "s");
    }

    /// <summary>
    /// Runs one tick inside a new root span named <c>&lt;worker&gt; tick</c> and records its outcome. The exception of
    /// a failed tick is rethrown; cancellation through <paramref name="ct"/> is recorded as <c>cancelled</c>, not
    /// <c>error</c>.
    /// </summary>
    /// <param name="worker">Low-cardinality worker name (the type name, plus the module for per-module workers).</param>
    /// <param name="tick">The tick body.</param>
    /// <param name="ct">The worker's stopping token, passed to <paramref name="tick"/>.</param>
    /// <returns>A task that completes with the tick.</returns>
    public async Task TrackTickAsync(string worker, Func<CancellationToken, Task> tick, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tick);

        // A tick is its own trace, never a child of whatever happened to be current when the worker was started
        // (host startup, or a test calling RunOnceAsync).
        var ambient = Activity.Current;
        Activity.Current = null;
        var activity = Source.StartActivity($"{worker} tick", ActivityKind.Internal);
        activity?.SetTag(WorkerTag, worker);

        var start = Stopwatch.GetTimestamp();
        var outcome = Success;
        try
        {
            await tick(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            outcome = Cancelled;
            throw;
        }
        catch (Exception ex)
        {
            outcome = Error;
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
        finally
        {
            activity?.SetTag(OutcomeTag, outcome);
            activity?.Dispose();
            Activity.Current = ambient;

            var tags = new TagList { { WorkerTag, worker }, { OutcomeTag, outcome } };
            _ticks.Add(1, tags);
            _duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
        }
    }
}
