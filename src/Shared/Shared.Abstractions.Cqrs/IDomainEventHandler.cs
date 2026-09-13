namespace Shared.Abstractions.Cqrs;

using System.Diagnostics.CodeAnalysis;
using Shared.Abstractions.Core.Domain;

[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The 'EventHandler' suffix names the handler of a domain/integration event, not a System.EventHandler delegate; the name is a public contract used by every module.")]
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken ct = default);
}
