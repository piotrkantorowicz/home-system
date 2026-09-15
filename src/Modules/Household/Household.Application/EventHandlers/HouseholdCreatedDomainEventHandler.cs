namespace Household.Application.EventHandlers;

using Household.Contracts.Events;
using Household.Domain.Events;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Messaging;

/// <summary>Maps <see cref="HouseholdCreatedDomainEvent"/> to its integration event and publishes it via the outbox.</summary>
internal sealed class HouseholdCreatedDomainEventHandler(IIntegrationEventBus bus, TimeProvider clock)
    : IDomainEventHandler<HouseholdCreatedDomainEvent>
{
    public Task HandleAsync(HouseholdCreatedDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return bus.PublishAsync(
            new HouseholdCreatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: clock.GetUtcNow().UtcDateTime,
                HouseholdId: domainEvent.HouseholdId.Value,
                OwnerPersonId: domainEvent.OwnerPersonId.Value),
            ct);
    }
}
