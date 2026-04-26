namespace Shared.Messaging.Tests.Transport;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;
using Shouldly;

public sealed class InProcessIntegrationEventTransportTests
{
    public sealed record TestEvent(Guid EventId, DateTime OccurredAt, string Payload) : IIntegrationEvent;

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
    public async Task DispatchAsync_WithRegisteredHandler_InvokesHandler()
    {
        var serializer = new IntegrationEventSerializer(new[] { "Shared." });
        TestEvent? captured = null;
        var handler = Substitute.For<IIntegrationEventHandler<TestEvent>>();
        await handler.HandleAsync(Arg.Do<TestEvent>(e => captured = e), Arg.Any<CancellationToken>());

        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer>(serializer);
        services.AddScoped<IIntegrationEventHandler<TestEvent>>(_ => handler);
        var sp = services.BuildServiceProvider();

        var sut = new InProcessIntegrationEventTransport(sp);
        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, serializer);

        await sut.DispatchAsync(message, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Payload.ShouldBe("x");
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlerRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer>(new IntegrationEventSerializer(new[] { "Shared." }));
        var sp = services.BuildServiceProvider();
        var sut = new InProcessIntegrationEventTransport(sp);

        var serializer = sp.GetRequiredService<IIntegrationEventSerializer>();
        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, serializer);

        var act = async () => await sut.DispatchAsync(message, CancellationToken.None);
        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_InvokesAll()
    {
        var serializer = new IntegrationEventSerializer(new[] { "Shared." });
        var h1 = Substitute.For<IIntegrationEventHandler<TestEvent>>();
        var h2 = Substitute.For<IIntegrationEventHandler<TestEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer>(serializer);
        services.AddScoped<IIntegrationEventHandler<TestEvent>>(_ => h1);
        services.AddScoped<IIntegrationEventHandler<TestEvent>>(_ => h2);
        var sp = services.BuildServiceProvider();

        var sut = new InProcessIntegrationEventTransport(sp);
        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, serializer);

        await sut.DispatchAsync(message, CancellationToken.None);

        await h1.Received(1).HandleAsync(Arg.Any<TestEvent>(), Arg.Any<CancellationToken>());
        await h2.Received(1).HandleAsync(Arg.Any<TestEvent>(), Arg.Any<CancellationToken>());
    }
}
