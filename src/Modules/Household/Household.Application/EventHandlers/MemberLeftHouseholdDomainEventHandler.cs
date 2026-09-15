namespace Household.Application.EventHandlers;

using Household.Contracts.Events;
using Household.Domain.Events;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Messaging;

/// <summary>Maps <see cref="MemberLeftHouseholdDomainEvent"/> to its integration event and publishes it via the outbox.</summary>
internal sealed class MemberLeftHouseholdDomainEventHandler(IIntegrationEventBus bus)
    : IDomainEventHandler<MemberLeftHouseholdDomainEvent>
{
    public Task HandleAsync(MemberLeftHouseholdDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return bus.PublishAsync(
            new MemberLeftHouseholdIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: DateTime.UtcNow,
                HouseholdId: domainEvent.HouseholdId.Value,
                PersonId: domainEvent.PersonId.Value),
            ct);
    }
}
