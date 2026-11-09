namespace Shared.Messaging.Tests.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Shared.Infrastructure.Messaging.Telemetry;

/// <summary>The tick span, counter and duration histogram recorded by <see cref="WorkerTelemetry"/>.</summary>
public sealed class WorkerTelemetryTests : IDisposable
{
    // Unique per test: the span listener is process-wide and other tests run workers in parallel.
    private readonly string _worker = $"TestWorker-{Guid.CreateVersion7():N}";

    private readonly List<Activity> _spans = [];
    private readonly ActivityListener _listener;
    private readonly ActivitySource _parentSource = new("Test.Parent");
    private readonly IMeterFactory _meters =
        new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
    private readonly MetricCollector<long> _ticks;
    private readonly MetricCollector<double> _duration;
    private readonly WorkerTelemetry _sut;

    /// <summary>Listens to the workers and parent sources and collects both instruments.</summary>
    public WorkerTelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name is WorkerTelemetry.Name or "Test.Parent",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (_spans) _spans.Add(a); },
        };
        ActivitySource.AddActivityListener(_listener);
        _ticks = new MetricCollector<long>(_meters, WorkerTelemetry.Name, "homesystem.worker.ticks");
        _duration = new MetricCollector<double>(_meters, WorkerTelemetry.Name, "homesystem.worker.tick.duration");
        _sut = new WorkerTelemetry(_meters);
    }

    /// <summary>Stops listening and disposes the meters.</summary>
    public void Dispose()
    {
        _listener.Dispose();
        _parentSource.Dispose();
        _ticks.Dispose();
        _duration.Dispose();
        _meters.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A tick that completes counts once with outcome <c>success</c> and records its duration.</summary>
    [Fact]
    public async Task TrackTickAsync_WhenTickCompletes_RecordsSuccess()
    {
        await _sut.TrackTickAsync(_worker, _ => Task.CompletedTask, Ct);

        var tick = _ticks.GetMeasurementSnapshot().ShouldHaveSingleItem();
        tick.Value.ShouldBe(1);
        tick.Tags[WorkerTelemetry.WorkerTag].ShouldBe(_worker);
        tick.Tags[WorkerTelemetry.OutcomeTag].ShouldBe(WorkerTelemetry.Success);
        var duration = _duration.GetMeasurementSnapshot().ShouldHaveSingleItem();
        duration.Value.ShouldBeGreaterThanOrEqualTo(0);
        duration.Tags[WorkerTelemetry.OutcomeTag].ShouldBe(WorkerTelemetry.Success);
        var span = TickSpan();
        span.GetTagItem(WorkerTelemetry.OutcomeTag).ShouldBe(WorkerTelemetry.Success);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    /// <summary>A tick that throws is rethrown, counts with outcome <c>error</c> and fails its span.</summary>
    [Fact]
    public async Task TrackTickAsync_WhenTickThrows_RecordsErrorAndRethrows()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.TrackTickAsync(_worker, _ => throw new InvalidOperationException("boom"), Ct));

        _ticks.GetMeasurementSnapshot().ShouldHaveSingleItem().Tags[WorkerTelemetry.OutcomeTag].ShouldBe(WorkerTelemetry.Error);
        _duration.GetMeasurementSnapshot().ShouldHaveSingleItem().Tags[WorkerTelemetry.OutcomeTag].ShouldBe(WorkerTelemetry.Error);
        var span = TickSpan();
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.GetTagItem(WorkerTelemetry.OutcomeTag).ShouldBe(WorkerTelemetry.Error);
    }

    /// <summary>Shutdown cancelling the tick is <c>cancelled</c>, not <c>error</c>.</summary>
    [Fact]
    public async Task TrackTickAsync_WhenStoppingTokenCancels_RecordsCancelled()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => _sut.TrackTickAsync(_worker, token => Task.FromCanceled(token), stopping.Token));

        _ticks.GetMeasurementSnapshot().ShouldHaveSingleItem().Tags[WorkerTelemetry.OutcomeTag].ShouldBe(WorkerTelemetry.Cancelled);
        TickSpan().Status.ShouldBe(ActivityStatusCode.Unset);
    }

    /// <summary>A cancellation that is not the stopping token (e.g. a provider timeout) is an <c>error</c>.</summary>
    [Fact]
    public async Task TrackTickAsync_WhenTickTimesOutWithoutShutdown_RecordsError()
    {
        await Should.ThrowAsync<TaskCanceledException>(
            () => _sut.TrackTickAsync(_worker, _ => throw new TaskCanceledException("timeout"), Ct));

        _ticks.GetMeasurementSnapshot().ShouldHaveSingleItem().Tags[WorkerTelemetry.OutcomeTag].ShouldBe(WorkerTelemetry.Error);
    }

    /// <summary>
    /// The tick span is a new root even when the caller runs under a span, it is current while the tick runs (so
    /// the tick's own spans are its children), and the caller's span is current again afterwards.
    /// </summary>
    [Fact]
    public async Task TrackTickAsync_UnderAmbientSpan_StartsRootSpanCurrentDuringTick()
    {
        using var ambient = _parentSource.StartActivity("ambient");
        ambient.ShouldNotBeNull();
        Activity? currentInTick = null;

        await _sut.TrackTickAsync(_worker, _ =>
        {
            currentInTick = Activity.Current;
            return Task.CompletedTask;
        }, Ct);

        var span = TickSpan();
        span.DisplayName.ShouldBe($"{_worker} tick");
        span.ParentSpanId.ShouldBe(default);
        span.TraceId.ShouldNotBe(ambient.TraceId);
        currentInTick.ShouldBeSameAs(span);
        Activity.Current.ShouldBeSameAs(ambient);
    }

    private Activity TickSpan()
    {
        lock (_spans)
            return _spans.Where(s => s.Source.Name == WorkerTelemetry.Name
                && Equals(s.GetTagItem(WorkerTelemetry.WorkerTag), _worker)).ShouldHaveSingleItem();
    }
}
