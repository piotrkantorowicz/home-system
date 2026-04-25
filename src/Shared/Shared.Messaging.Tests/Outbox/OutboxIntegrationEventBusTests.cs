namespace Shared.Messaging.Tests.Outbox;

using NSubstitute;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shouldly;

public sealed class OutboxIntegrationEventBusTests
{
    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly IIntegrationEventSerializer _serializer = Substitute.For<IIntegrationEventSerializer>();
    private readonly OutboxIntegrationEventBus _sut;

    public OutboxIntegrationEventBusTests()
        => _sut = new OutboxIntegrationEventBus(_store, _serializer);

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

    private sealed record TestIntegrationEvent(Guid EventId, DateTime OccurredAt, string Payload)
        : IIntegrationEvent;
}
