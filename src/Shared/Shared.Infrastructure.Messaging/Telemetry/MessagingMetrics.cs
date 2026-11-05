namespace Shared.Infrastructure.Messaging.Telemetry;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

/// <summary>
/// Outbox and inbox instruments (<c>HomeSystem.Messaging</c> meter). Tags carry module, event and handler
/// type names and outcomes only — never payload data. Singleton, created through <see cref="IMeterFactory"/>.
/// </summary>
public sealed class MessagingMetrics
{
    /// <summary>Meter name; the host subscribes to <c>HomeSystem.*</c>.</summary>
    public const string MeterName = "HomeSystem.Messaging";

    private readonly Histogram<double> _publishDuration;
    private readonly Counter<long> _deadLetters;
    private readonly Histogram<double> _inboxDuration;
    private readonly ConcurrentDictionary<string, (long Count, double OldestAgeSeconds)> _backlog = new();

    /// <summary>Creates the meter and its instruments.</summary>
    /// <param name="meters">Factory that owns the meter's lifetime.</param>
    public MessagingMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(MeterName);
        meter.CreateObservableGauge(
            "homesystem.outbox.pending",
            () => _backlog.Select(x => new Measurement<long>(x.Value.Count, new KeyValuePair<string, object?>("homesystem.module", x.Key))),
            unit: "{message}");
        meter.CreateObservableGauge(
            "homesystem.outbox.oldest_pending_age",
            () => _backlog.Select(x => new Measurement<double>(x.Value.OldestAgeSeconds, new KeyValuePair<string, object?>("homesystem.module", x.Key))),
            unit: "s");
        _publishDuration = meter.CreateHistogram<double>("homesystem.outbox.publish.duration", unit: "s");
        _deadLetters = meter.CreateCounter<long>("homesystem.outbox.dead_letters", unit: "{message}");
        _inboxDuration = meter.CreateHistogram<double>("homesystem.inbox.handle.duration", unit: "s");
    }

    /// <summary>Stores the latest backlog of one module; the gauges report it on the next collection.</summary>
    /// <param name="module">Publishing module name.</param>
    /// <param name="pending">Undelivered messages with attempts left.</param>
    /// <param name="oldestAge">Age of the oldest of them.</param>
    public void SetBacklog(string module, long pending, TimeSpan oldestAge)
        => _backlog[module] = (pending, oldestAge.TotalSeconds);

    /// <summary>Records one dispatch of an outbox message (outcome <c>published</c> or <c>failed</c>).</summary>
    /// <param name="eventType">Assembly-qualified event type name.</param>
    /// <param name="outcome">Dispatch outcome.</param>
    /// <param name="elapsed">Dispatch duration.</param>
    public void RecordPublish(string eventType, string outcome, TimeSpan elapsed)
        => _publishDuration.Record(elapsed.TotalSeconds,
            new("homesystem.event.type", ShortName(eventType)), new("outcome", outcome));

    /// <summary>Counts a message that just used up its last attempt.</summary>
    /// <param name="module">Publishing module name.</param>
    /// <param name="eventType">Assembly-qualified event type name.</param>
    public void RecordDeadLetter(string module, string eventType)
        => _deadLetters.Add(1,
            new("homesystem.module", module), new("homesystem.event.type", ShortName(eventType)));

    /// <summary>Records one inbox-wrapped handler run (outcome <c>handled</c>, <c>duplicate</c> or <c>error</c>).</summary>
    /// <param name="eventType">Event type name.</param>
    /// <param name="handler">Handler type name.</param>
    /// <param name="outcome">Handling outcome.</param>
    /// <param name="elapsed">Duration including the inbox transaction.</param>
    public void RecordInbox(string eventType, string handler, string outcome, TimeSpan elapsed)
        => _inboxDuration.Record(elapsed.TotalSeconds,
            new("homesystem.event.type", ShortName(eventType)),
            new("homesystem.handler.type", handler),
            new("outcome", outcome));

    // "Ns.Type, Assembly, Version=..." -> "Type"
    private static string ShortName(string typeName)
    {
        var comma = typeName.IndexOf(',', StringComparison.Ordinal);
        var name = comma < 0 ? typeName : typeName[..comma];
        return name[(name.LastIndexOfAny(['.', '+']) + 1)..];
    }
}
