namespace Shared.Messaging.Tests.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Transport;

public sealed class OutboxWorkerTests
{
    private static OutboxMessage MakePending(Guid id) =>
        new(id, Guid.NewGuid(), "Some.Event, Some.Asm", "{}", DateTime.UtcNow, null, 0, null);

    private static IServiceScopeFactory ScopeFactoryWith(IOutboxStore store, IIntegrationEventTransport transport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(transport);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task RunOnceAsync_WithPendingMessages_DispatchesAndMarksProcessed()
    {
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([msg]);

        var transport = Substitute.For<IIntegrationEventTransport>();

        var sut = new OutboxWorker(
            ScopeFactoryWith(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        await transport.Received(1).DispatchAsync(msg, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(msg.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_RecordsFailureAndContinues()
    {
        var msg1 = MakePending(Guid.NewGuid());
        var msg2 = MakePending(Guid.NewGuid());

        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([msg1, msg2]);

        var transport = Substitute.For<IIntegrationEventTransport>();
        transport
            .When(t => t.DispatchAsync(msg1, Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException("boom"));

        var sut = new OutboxWorker(
            ScopeFactoryWith(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        await store.Received(1).RecordFailureAsync(msg1.Id, "boom", Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkProcessedAsync(msg1.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await transport.Received(1).DispatchAsync(msg2, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(msg2.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_WithNoPendingMessages_DoesNothing()
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([]);

        var transport = Substitute.For<IIntegrationEventTransport>();

        var sut = new OutboxWorker(
            ScopeFactoryWith(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        await transport.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }
}
