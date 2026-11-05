namespace Assistant.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// Captures the finished spans of one trace. Requests carry <see cref="Traceparent"/>, so spans of tests
/// running in parallel never mix in.
/// </summary>
public sealed class SpanProbe : IDisposable
{
    private readonly ActivityListener _listener;

    /// <summary>Starts listening for the assistant's spans and the server span that carries the trace id.</summary>
    public SpanProbe()
    {
        ActivityTraceId traceId = ActivityTraceId.CreateRandom();
        Traceparent = $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01";
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "HomeSystem.Assistant" or "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.TraceId == traceId)
                    Spans.Enqueue(a);
            },
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>Value of the <c>traceparent</c> header to send with the request under test.</summary>
    public string Traceparent { get; }

    /// <summary>Finished spans of the trace, in the order they stopped.</summary>
    public ConcurrentQueue<Activity> Spans { get; } = new();

    /// <summary>Adds the trace header to <paramref name="client"/>.</summary>
    /// <param name="client">The caller.</param>
    /// <returns><paramref name="client"/>.</returns>
    public HttpClient Trace(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("traceparent", Traceparent);
        return client;
    }

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();
}
