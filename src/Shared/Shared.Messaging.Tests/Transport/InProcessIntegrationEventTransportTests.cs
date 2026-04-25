namespace Shared.Messaging.Tests.Transport;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;

public sealed class InProcessIntegrationEventTransportTests
{
    private static OutboxMessage MakeMessage<TEvent>(TEvent @event, IIntegrationEventSerializer serializer)
        where TEvent : IIntegrationEvent
        => new(
            Id: Guid.NewGuid(),
            EventId: @event.EventId,
            EventType: typeof(TEvent).AssemblyQualifiedName!,
            Payload: serializer.Serialize(@event),
            OccurredAt: @event.OccurredAt,
            ProcessedAt: null,
            AttemptCount: 0,
            LastError: null);

    [Fact]
    public async Task DispatchAsync_WithRegisteredHandler_ResolvesViaInboxExecutorAndInvokesHandler()
    {
        var serializer = new IntegrationEventSerializer();
        var capturedEvent = (TestEvent?)null;

        var handler = Substitute.For<IIntegrationEventHandler<TestEvent>>();
        await handler.HandleAsync(Arg.Do<TestEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>());

        var inbox = Substitute.For<IInboxExecutor>();
        inbox
            .ExecuteAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var invocation = ci.Arg<Func<CancellationToken, Task>>();
                await invocation(CancellationToken.None);
            });

        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer>(serializer);
        services.AddScoped<IIntegrationEventHandler<TestEvent>>(_ => handler);
        services.AddScoped<IInboxExecutor>(_ => inbox);
        var sp = services.BuildServiceProvider();

        var sut = new InProcessIntegrationEventTransport(sp);
        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, serializer);

        await sut.DispatchAsync(message, CancellationToken.None);

        capturedEvent.ShouldNotBeNull();
        capturedEvent!.Payload.ShouldBe("x");

        await inbox.Received(1).ExecuteAsync(
            @event.EventId, message.EventType, Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlerRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.AddScoped<IInboxExecutor>(_ => Substitute.For<IInboxExecutor>());
        var sp = services.BuildServiceProvider();
        var sut = new InProcessIntegrationEventTransport(sp);

        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, sp.GetRequiredService<IIntegrationEventSerializer>());

        var act = async () => await sut.DispatchAsync(message, CancellationToken.None);

        await act.ShouldNotThrowAsync();
    }

    public sealed record TestEvent(Guid EventId, DateTime OccurredAt, string Payload) : IIntegrationEvent;
}
