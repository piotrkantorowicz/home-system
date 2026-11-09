namespace Shared.Infrastructure.Messaging.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Shared.Abstractions.Core.Telemetry;

/// <summary>
/// Measures background worker ticks (<c>HomeSystem.Workers</c> source and meter): one root span per tick, so the
/// database calls a tick makes have a parent, plus a tick counter and a duration histogram tagged by worker and
/// outcome. Singleton, created through <see cref="IMeterFactory"/> and registered once by the host. Conventions: docs/rules/backend-observability.md.
/// </summary>
public sealed class WorkerTelemetry : IWorkerTelemetry
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

    /// <inheritdoc />
    public async Task TrackTickAsync(string worker, Func<CancellationToken, Task> tick, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tick);

        // A tick is its own trace, never a child of whatever happened to be current when the worker was started
        // (host startup, or a test calling RunOnceAsync).
        var ambient = Activity.Current;
        Activity.Current = null;
        try
        {
            using var activity = Source.StartActivity($"{worker} tick", ActivityKind.Internal);
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

                var tags = new TagList { { WorkerTag, worker }, { OutcomeTag, outcome } };
                _ticks.Add(1, tags);
                _duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
            }
        }
        finally
        {
            // After the span is disposed (stopping it resets Current to its null parent).
            Activity.Current = ambient;
        }
    }
}
