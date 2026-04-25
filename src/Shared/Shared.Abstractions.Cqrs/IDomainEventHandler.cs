namespace Shared.Abstractions.Cqrs;

using Shared.Abstractions.Core.Domain;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken ct = default);
}
