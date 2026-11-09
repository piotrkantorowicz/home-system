namespace Shared.Messaging.Tests.Outbox;

using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Telemetry;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Telemetry;
using Shared.Infrastructure.Messaging.Transport;
using Shouldly;

/// <summary>Unit tests for <c>OutboxWorker</c>: the store and transport are substituted and the worker is driven through <c>RunOnceAsync</c>.</summary>
public sealed class OutboxWorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly FakeTimeProvider Clock = new(Now);

    // A dummy DbContext type to satisfy the generic constraint of OutboxWorker<TDbContext>.
    /// <summary>Placeholder <c>DbContext</c> that only satisfies the worker's generic constraint; never opened.</summary>
    public sealed class TestDbContext : DbContext
    {
        /// <summary>Creates the context.</summary>
        /// <param name="options">Options supplied by the test.</param>
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    private static OutboxMessage MakePending(Guid id) =>
        new(id, Guid.NewGuid(), "Some.Event, Some.Asm", "{}", Now.UtcDateTime, null, 0, null);

    private static IServiceScopeFactory ScopeFactory(IOutboxStore? store, IIntegrationEventTransport? transport)
    {
        var services = new ServiceCollection();
        if (store is not null)
            services.AddKeyedSingleton<IOutboxStore>(typeof(TestDbContext), (_, _) => store);
        if (transport is not null)
            services.AddSingleton(transport);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    private static OutboxWorker<TestDbContext> SutWith(
        IOutboxStore? store,
        IIntegrationEventTransport? transport,
        ILogger<OutboxWorker<TestDbContext>>? logger = null,
        IMeterFactory? meters = null,
        TimeProvider? clock = null) =>
        new(ScopeFactory(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            logger ?? NullLogger<OutboxWorker<TestDbContext>>.Instance,
            clock ?? Clock,
            new WorkerTelemetry(new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>()),
            new MessagingMetrics(meters ?? NewMeters()));

    private static IMeterFactory NewMeters()
        => new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    private static IIntegrationEventTransport Throwing(OutboxMessage msg)
    {
        var transport = Substitute.For<IIntegrationEventTransport>();
        transport.When(t => t.DispatchAsync(msg, Arg.Any<CancellationToken>())).Throw(new InvalidOperationException("boom"));
        return transport;
    }

    /// <summary>With pending messages: <c>RunOnceAsync</c> dispatches and marks processed.</summary>
    [Fact]
    public async Task RunOnceAsync_WithPendingMessages_DispatchesAndMarksProcessed()
    {
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([msg]);
        var transport = Substitute.For<IIntegrationEventTransport>();

        await SutWith(store, transport).RunOnceAsync(TestContext.Current.CancellationToken);

        await transport.Received(1).DispatchAsync(msg, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(msg.Id, Now.UtcDateTime, Arg.Any<CancellationToken>());
    }

    /// <summary><c>RunOnceAsync</c> asks the store only for messages below the attempt limit whose backoff has elapsed.</summary>
    [Fact]
    public async Task RunOnceAsync_PassesMaxAttemptsToStore()
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);

        await SutWith(store, Substitute.For<IIntegrationEventTransport>()).RunOnceAsync(TestContext.Current.CancellationToken);

        await store.Received(1).GetUnprocessedAsync(
            Arg.Any<int>(), new OutboxWorkerOptions().MaxAttempts, Now.UtcDateTime, Arg.Any<CancellationToken>());
    }

    /// <summary>When transport throws: <c>RunOnceAsync</c> records failure and continues.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_RecordsFailureAndContinues()
    {
        var m1 = MakePending(Guid.NewGuid());
        var m2 = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([m1, m2]);
        var transport = Substitute.For<IIntegrationEventTransport>();
        transport.When(t => t.DispatchAsync(m1, Arg.Any<CancellationToken>()))
                 .Throw(new InvalidOperationException("boom"));

        await SutWith(store, transport).RunOnceAsync(TestContext.Current.CancellationToken);

        await store.Received(1).RecordFailureAsync(m1.Id, "boom", Now.UtcDateTime.AddSeconds(1), Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkProcessedAsync(m1.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await transport.Received(1).DispatchAsync(m2, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(m2.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A failure schedules the retry with exponential backoff from the message's attempt count.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_SchedulesRetryWithBackoff()
    {
        var msg = MakePending(Guid.NewGuid()) with { AttemptCount = 3 };
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([msg]);
        var transport = Substitute.For<IIntegrationEventTransport>();
        transport.When(t => t.DispatchAsync(msg, Arg.Any<CancellationToken>())).Throw(new InvalidOperationException("boom"));

        await SutWith(store, transport).RunOnceAsync(TestContext.Current.CancellationToken);

        // Fourth failure: 1 s × 2^3.
        await store.Received(1).RecordFailureAsync(msg.Id, "boom", Now.UtcDateTime.AddSeconds(8), Arg.Any<CancellationToken>());
    }

    /// <summary>The retry delay doubles per failure and stops at the configured maximum.</summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 16)]
    [InlineData(9, 256)]
    [InlineData(10, 300)]
    [InlineData(40, 300)]
    public void RetryDelay_DoublesAndCaps(int failures, int expectedSeconds)
        => SutWith(null, null).RetryDelay(failures).ShouldBe(TimeSpan.FromSeconds(expectedSeconds));

    /// <summary>When transport throws: <c>RunOnceAsync</c> logs one error with the exception and message id.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_LogsOneErrorWithExceptionAndMessageId()
    {
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([msg]);
        var transport = Substitute.For<IIntegrationEventTransport>();
        var boom = new InvalidOperationException("boom");
        transport.When(t => t.DispatchAsync(msg, Arg.Any<CancellationToken>())).Throw(boom);
        var logger = new FakeLogger<OutboxWorker<TestDbContext>>();

        await SutWith(store, transport, logger).RunOnceAsync(TestContext.Current.CancellationToken);

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Id.Id.ShouldBe(0);
        record.Exception.ShouldBeSameAs(boom);
        record.Message.ShouldContain(msg.Id.ToString());
        record.Message.ShouldContain(nameof(TestDbContext));
        record.StructuredState.ShouldNotBeNull()
            .ShouldContain(kv => kv.Key == "MessageId" && kv.Value == msg.Id.ToString());
    }

    /// <summary>With no pending messages: <c>RunOnceAsync</c> does nothing.</summary>
    [Fact]
    public async Task RunOnceAsync_WithNoPendingMessages_DoesNothing()
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        var transport = Substitute.For<IIntegrationEventTransport>();

        await SutWith(store, transport).RunOnceAsync(TestContext.Current.CancellationToken);

        await transport.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>With no store registered: <c>RunOnceAsync</c> returns without throwing.</summary>
    [Fact]
    public async Task RunOnceAsync_WithNoStoreRegistered_ReturnsWithoutThrowing()
    {
        var act = () => SutWith(null, null).RunOnceAsync(TestContext.Current.CancellationToken);
        await act.ShouldNotThrowAsync();
    }

    /// <summary>A message that fails its last allowed attempt increments the dead-letter counter.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenLastAttemptFails_IncrementsDeadLetters()
    {
        var meters = NewMeters();
        using var deadLetters = new MetricCollector<long>(meters, MessagingMetrics.MeterName, "homesystem.outbox.dead_letters");
        var msg = MakePending(Guid.NewGuid()) with { AttemptCount = new OutboxWorkerOptions().MaxAttempts - 1 };
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([msg]);

        await SutWith(store, Throwing(msg), meters: meters).RunOnceAsync(TestContext.Current.CancellationToken);

        var m = deadLetters.GetMeasurementSnapshot().ShouldHaveSingleItem();
        m.Value.ShouldBe(1);
        m.Tags["homesystem.event.type"].ShouldBe("Event");
    }

    /// <summary>A failure with attempts left does not count as a dead letter.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenAttemptsRemain_DoesNotIncrementDeadLetters()
    {
        var meters = NewMeters();
        using var deadLetters = new MetricCollector<long>(meters, MessagingMetrics.MeterName, "homesystem.outbox.dead_letters");
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([msg]);

        await SutWith(store, Throwing(msg), meters: meters).RunOnceAsync(TestContext.Current.CancellationToken);

        deadLetters.GetMeasurementSnapshot().ShouldBeEmpty();
    }

    /// <summary>Each dispatch records the publish duration tagged with event type and outcome.</summary>
    [Fact]
    public async Task RunOnceAsync_RecordsPublishDurationWithOutcome()
    {
        var meters = NewMeters();
        using var duration = new MetricCollector<double>(meters, MessagingMetrics.MeterName, "homesystem.outbox.publish.duration");
        var ok = MakePending(Guid.NewGuid());
        var bad = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([ok, bad]);

        await SutWith(store, Throwing(bad), meters: meters).RunOnceAsync(TestContext.Current.CancellationToken);

        duration.GetMeasurementSnapshot().Select(x => (string?)x.Tags["outcome"]).ShouldBe(["published", "failed"]);
    }

    /// <summary>The backlog gauges report what the store counts, even when nothing was dispatched.</summary>
    [Fact]
    public async Task RunOnceAsync_ReportsBacklogGauges()
    {
        var meters = NewMeters();
        using var pending = new MetricCollector<long>(meters, MessagingMetrics.MeterName, "homesystem.outbox.pending");
        using var age = new MetricCollector<double>(meters, MessagingMetrics.MeterName, "homesystem.outbox.oldest_pending_age");
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new OutboxPending(3, Now.UtcDateTime.AddSeconds(-90)));

        await SutWith(store, Substitute.For<IIntegrationEventTransport>(), meters: meters)
            .RunOnceAsync(TestContext.Current.CancellationToken);

        pending.RecordObservableInstruments();
        age.RecordObservableInstruments();
        pending.LastMeasurement!.Value.ShouldBe(3);
        pending.LastMeasurement.Tags["homesystem.module"].ShouldBe("Test");
        age.LastMeasurement!.Value.ShouldBe(90);
    }

    /// <summary>If marking a delivered message processed fails, it is counted once as failed and never as published.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenMarkProcessedFails_RecordsOnlyFailed()
    {
        var meters = NewMeters();
        using var duration = new MetricCollector<double>(meters, MessagingMetrics.MeterName, "homesystem.outbox.publish.duration");
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([msg]);
        store.MarkProcessedAsync(msg.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("db")));

        await SutWith(store, Substitute.For<IIntegrationEventTransport>(), meters: meters)
            .RunOnceAsync(TestContext.Current.CancellationToken);

        duration.GetMeasurementSnapshot().ShouldHaveSingleItem().Tags["outcome"].ShouldBe("failed");
    }

    /// <summary>A tick that throws still refreshes the backlog gauges.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenTickThrows_StillReportsBacklog()
    {
        var meters = NewMeters();
        using var pending = new MetricCollector<long>(meters, MessagingMetrics.MeterName, "homesystem.outbox.pending");
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OutboxMessage>>(new InvalidOperationException("db")));
        store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new OutboxPending(4, null));

        var act = () => SutWith(store, Substitute.For<IIntegrationEventTransport>(), meters: meters)
            .RunOnceAsync(TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();

        pending.RecordObservableInstruments();
        pending.LastMeasurement!.Value.ShouldBe(4);
    }

    private static IOutboxStore IdleStore()
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new OutboxPending(0, null));
        return store;
    }

    /// <summary>Idle ticks inside the interval do not repeat the backlog query; it runs again once the interval has passed.</summary>
    [Fact]
    public async Task RunOnceAsync_IdleTicksWithinInterval_QueryBacklogOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeTimeProvider(Now);
        var store = IdleStore();
        var sut = SutWith(store, Substitute.For<IIntegrationEventTransport>(), clock: clock);

        await sut.RunOnceAsync(ct);
        clock.Advance(TimeSpan.FromSeconds(1));
        await sut.RunOnceAsync(ct);
        await sut.RunOnceAsync(ct);
        await store.Received(1).GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

        clock.Advance(TimeSpan.FromSeconds(15));
        await sut.RunOnceAsync(ct);
        await store.Received(2).GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A tick that ends because the host is stopping does not query the backlog.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenCancelled_SkipsBacklogQuery()
    {
        using var cts = new CancellationTokenSource();
        var store = IdleStore();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return (IReadOnlyList<OutboxMessage>)[];
            });

        await SutWith(store, Substitute.For<IIntegrationEventTransport>(), clock: new FakeTimeProvider(Now))
            .RunOnceAsync(cts.Token);

        await store.DidNotReceive().GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A backlog query that throws, even with <c>OperationCanceledException</c>, never replaces the tick's own exception.</summary>
    [Theory]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task RunOnceAsync_WhenBacklogQueryThrows_KeepsTheTickException(Type backlogFailure)
    {
        var store = IdleStore();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OutboxMessage>>(new TimeoutException("tick")));
        store.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<OutboxPending>((Exception)Activator.CreateInstance(backlogFailure)!));

        var act = () => SutWith(store, Substitute.For<IIntegrationEventTransport>(), clock: new FakeTimeProvider(Now))
            .RunOnceAsync(TestContext.Current.CancellationToken);

        (await act.ShouldThrowAsync<TimeoutException>()).Message.ShouldBe("tick");
    }
}
