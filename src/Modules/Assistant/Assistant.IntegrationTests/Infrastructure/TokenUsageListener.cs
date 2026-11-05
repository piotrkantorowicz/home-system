namespace Assistant.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

/// <summary>
/// Reads <c>gen_ai.client.token.usage</c> by name from any meter. <c>MetricCollector</c> cannot be used here:
/// the chat wrapper's meter is not scoped to the host's <c>IMeterFactory</c> (it matched nothing once several
/// hosts ran in one process). Assert with <c>ShouldContain</c> only; other hosts' measurements may appear too.
/// </summary>
public sealed class TokenUsageListener : IDisposable
{
    private readonly MeterListener _listener = new();

    /// <summary>Starts listening.</summary>
    public TokenUsageListener()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "gen_ai.client.token.usage")
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "gen_ai.request.model" && tag.Value is string model)
                    Models.Enqueue(model);
            }
        });
        _listener.Start();
    }

    /// <summary>The <c>gen_ai.request.model</c> of every token measurement seen.</summary>
    public ConcurrentQueue<string> Models { get; } = new();

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();
}
