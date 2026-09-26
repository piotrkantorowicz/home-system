namespace Shared.Messaging.Tests.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shared.Infrastructure.Messaging.Outbox;
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
        ILogger<OutboxWorker<TestDbContext>>? logger = null) =>
        new(ScopeFactory(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            logger ?? NullLogger<OutboxWorker<TestDbContext>>.Instance,
            Clock);

    /// <summary>With pending messages: <c>RunOnceAsync</c> dispatches and marks processed.</summary>
    [Fact]
    public async Task RunOnceAsync_WithPendingMessages_DispatchesAndMarksProcessed()
    {
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([msg]);
        var transport = Substitute.For<IIntegrationEventTransport>();

        await SutWith(store, transport).RunOnceAsync(TestContext.Current.CancellationToken);

        await transport.Received(1).DispatchAsync(msg, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(msg.Id, Now.UtcDateTime, Arg.Any<CancellationToken>());
    }

    /// <summary><c>RunOnceAsync</c> asks the store only for messages below the configured attempt limit.</summary>
    [Fact]
    public async Task RunOnceAsync_PassesMaxAttemptsToStore()
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        await SutWith(store, Substitute.For<IIntegrationEventTransport>()).RunOnceAsync(TestContext.Current.CancellationToken);

        await store.Received(1).GetUnprocessedAsync(
            Arg.Any<int>(), new OutboxWorkerOptions().MaxAttempts, Arg.Any<CancellationToken>());
    }

    /// <summary>When transport throws: <c>RunOnceAsync</c> records failure and continues.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_RecordsFailureAndContinues()
    {
        var m1 = MakePending(Guid.NewGuid());
        var m2 = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([m1, m2]);
        var transport = Substitute.For<IIntegrationEventTransport>();
        transport.When(t => t.DispatchAsync(m1, Arg.Any<CancellationToken>()))
                 .Throw(new InvalidOperationException("boom"));

        await SutWith(store, transport).RunOnceAsync(TestContext.Current.CancellationToken);

        await store.Received(1).RecordFailureAsync(m1.Id, "boom", Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkProcessedAsync(m1.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await transport.Received(1).DispatchAsync(m2, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(m2.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    /// <summary>With no pending messages: <c>RunOnceAsync</c> does nothing.</summary>
    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_LogsOneErrorWithExceptionAndMessageId()
    {
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([msg]);
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
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
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
}
