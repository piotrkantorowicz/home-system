namespace Shared.Messaging.Tests.Outbox;

using NSubstitute;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shouldly;

/// <summary>Unit tests for <c>OutboxIntegrationEventBus</c>: the outbox store is substituted to capture what is published.</summary>
public sealed class OutboxIntegrationEventBusTests
{
    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly IIntegrationEventSerializer _serializer = Substitute.For<IIntegrationEventSerializer>();
    private readonly OutboxIntegrationEventBus _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public OutboxIntegrationEventBusTests()
        => _sut = new OutboxIntegrationEventBus(_store, _serializer);

    /// <summary>With event: <c>PublishAsync</c> adds serialized outbox message.</summary>
    [Fact]
    public async Task PublishAsync_WithEvent_AddsSerializedOutboxMessage()
    {
        var eventId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 4, 25, 12, 0, 0, DateTimeKind.Utc);
        var @event = new TestIntegrationEvent(eventId, occurredAt, "hello");

        _serializer.Serialize(@event).Returns("""{"EventId":"...","Payload":"hello"}""");

        await _sut.PublishAsync(@event, CancellationToken.None);

        await _store.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.EventId == eventId &&
                m.OccurredAt == occurredAt &&
                m.EventType == typeof(TestIntegrationEvent).AssemblyQualifiedName &&
                m.Payload == """{"EventId":"...","Payload":"hello"}""" &&
                m.ProcessedAt == null &&
                m.AttemptCount == 0 &&
                m.LastError == null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>With base reference: <c>PublishAsync</c> records runtime type.</summary>
    [Fact]
    public async Task PublishAsync_WithBaseReference_RecordsRuntimeType()
    {
        // Caller publishes via base IIntegrationEvent reference — bus must capture runtime type.
        IIntegrationEvent @event = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        _serializer.Serialize(Arg.Any<IIntegrationEvent>()).Returns("{}");

        await _sut.PublishAsync(@event, CancellationToken.None);

        await _store.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m => m.EventType == typeof(TestIntegrationEvent).AssemblyQualifiedName),
            Arg.Any<CancellationToken>());
    }

    /// <summary>With null event: <c>PublishAsync</c> throws.</summary>
    [Fact]
    public async Task PublishAsync_WithNullEvent_Throws()
    {
        var act = () => _sut.PublishAsync<TestIntegrationEvent>(null!, CancellationToken.None);
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    private sealed record TestIntegrationEvent(Guid EventId, DateTime OccurredAt, string Payload)
        : IIntegrationEvent;
}
