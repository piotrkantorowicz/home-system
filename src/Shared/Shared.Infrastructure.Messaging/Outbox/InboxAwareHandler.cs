namespace Shared.Infrastructure.Messaging.Outbox;

using System.Diagnostics;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Telemetry;

internal sealed class InboxAwareHandler<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly IIntegrationEventHandler<TEvent> _inner;
    private readonly IInboxExecutor _inbox;
    private readonly MessagingMetrics _metrics;

    public InboxAwareHandler(IIntegrationEventHandler<TEvent> inner, IInboxExecutor inbox, MessagingMetrics metrics)
        => (_inner, _inbox, _metrics) = (inner, inbox, metrics);

    public async Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default)
    {
        // The executor skips the invocation for an already-consumed event, so "not invoked" == duplicate.
        var invoked = false;
        var outcome = "error";
        var started = Stopwatch.GetTimestamp();
        try
        {
            await _inbox.ExecuteAsync(
                eventId: integrationEvent.EventId,
                eventType: integrationEvent.GetType().AssemblyQualifiedName!,
                handlerInvocation: invocationCt =>
                {
                    invoked = true;
                    return _inner.HandleAsync(integrationEvent, invocationCt);
                },
                ct: ct).ConfigureAwait(false);
            outcome = invoked ? "handled" : "duplicate";
        }
        finally
        {
            _metrics.RecordInbox(
                typeof(TEvent).Name, _inner.GetType().Name, outcome, Stopwatch.GetElapsedTime(started));
        }
    }
}
