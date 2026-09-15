namespace Household.Application.EventHandlers;

using Household.Contracts.Events;
using Household.Domain.Events;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Messaging;

/// <summary>Maps <see cref="MemberJoinedHouseholdDomainEvent"/> to its integration event and publishes it via the outbox.</summary>
internal sealed class MemberJoinedHouseholdDomainEventHandler(IIntegrationEventBus bus, TimeProvider clock)
    : IDomainEventHandler<MemberJoinedHouseholdDomainEvent>
{
    public Task HandleAsync(MemberJoinedHouseholdDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return bus.PublishAsync(
            new MemberJoinedHouseholdIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: clock.GetUtcNow().UtcDateTime,
                HouseholdId: domainEvent.HouseholdId.Value,
                PersonId: domainEvent.PersonId.Value,
                Role: domainEvent.Role.ToString()),
            ct);
    }
}
