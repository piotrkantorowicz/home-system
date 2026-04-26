namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Messaging;

internal sealed class InboxAwareHandler<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly IIntegrationEventHandler<TEvent> _inner;
    private readonly IInboxExecutor _inbox;

    public InboxAwareHandler(IIntegrationEventHandler<TEvent> inner, IInboxExecutor inbox)
        => (_inner, _inbox) = (inner, inbox);

    public Task HandleAsync(TEvent @event, CancellationToken ct = default)
        => _inbox.ExecuteAsync(
            eventId: @event.EventId,
            eventType: @event.GetType().AssemblyQualifiedName!,
            handlerInvocation: invocationCt => _inner.HandleAsync(@event, invocationCt),
            ct: ct);
}
