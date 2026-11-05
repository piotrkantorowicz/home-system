namespace Assistant.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>Captures finished <c>HomeSystem.Assistant</c> spans and the counters and histograms of one host.</summary>
public sealed class TelemetryProbe : IDisposable
{
    private readonly ActivityListener _spans;
    private readonly MeterListener _metrics = new();

    /// <summary>Starts listening.</summary>
    /// <param name="meters">The host's meter factory; only its meters are read, so parallel hosts do not mix.</param>
    public TelemetryProbe(IMeterFactory meters)
    {
        _spans = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "HomeSystem.Assistant",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = Spans.Enqueue,
        };
        ActivitySource.AddActivityListener(_spans);

        _metrics.InstrumentPublished = (instrument, listener) =>
        {
            // The tool and turn counters come from the host's factory; the GenAI metrics from the chat wrapper's own meter.
            if (instrument.Meter.Name == "HomeSystem.Assistant" && (instrument.Meter.Scope == meters || instrument.Name.StartsWith("gen_ai", StringComparison.Ordinal)))
                listener.EnableMeasurementEvents(instrument);
        };
        _metrics.SetMeasurementEventCallback<long>(Record);
        _metrics.SetMeasurementEventCallback<int>(Record);
        _metrics.SetMeasurementEventCallback<double>(Record);
        _metrics.Start();
    }

    /// <summary>Finished spans, in the order they stopped.</summary>
    public ConcurrentQueue<Activity> Spans { get; } = new();

    /// <summary>Every measurement taken: instrument name and tags.</summary>
    public ConcurrentQueue<(string Instrument, Dictionary<string, object?> Tags)> Measurements { get; } = new();

    /// <summary>The tags of measurements of one instrument.</summary>
    /// <param name="instrument">Instrument name.</param>
    /// <returns>One tag set per measurement.</returns>
    public List<Dictionary<string, object?>> Of(string instrument) => [.. Measurements.Where(m => m.Instrument == instrument).Select(m => m.Tags)];

    /// <inheritdoc />
    public void Dispose()
    {
        _spans.Dispose();
        _metrics.Dispose();
    }

    private void Record<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
        => Measurements.Enqueue((instrument.Name, tags.ToArray().ToDictionary(t => t.Key, t => t.Value)));
}
