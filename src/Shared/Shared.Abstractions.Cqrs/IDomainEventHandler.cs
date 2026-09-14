namespace Shared.Abstractions.Cqrs;

using System.Diagnostics.CodeAnalysis;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Reacts to a domain event inside the module that raised it. Handlers run synchronously from the
/// persistence layer's save interceptor, before the aggregate is flushed and in the same
/// transaction as its write — do not query the database for the change, and keep side effects
/// transactional because the save can still fail; the usual
/// job is to map the event to an integration event and publish it via <c>IIntegrationEventBus</c>,
/// which writes to the module's outbox atomically with the change.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event this handler consumes.</typeparam>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The 'EventHandler' suffix names the handler of a domain/integration event, not a System.EventHandler delegate; the name is a public contract used by every module.")]
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>Handles the event; throwing rolls back the aggregate write it belongs to.</summary>
    /// <param name="domainEvent">The event raised by the aggregate.</param>
    /// <param name="ct">Propagates cancellation to every I/O call.</param>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken ct = default);
}
