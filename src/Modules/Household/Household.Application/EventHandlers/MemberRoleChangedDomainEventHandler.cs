namespace Household.Application.EventHandlers;

using Household.Contracts.Events;
using Household.Domain.Events;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Messaging;

/// <summary>Maps <see cref="MemberRoleChangedDomainEvent"/> to its integration event and publishes it via the outbox.</summary>
internal sealed class MemberRoleChangedDomainEventHandler(IIntegrationEventBus bus, TimeProvider clock)
    : IDomainEventHandler<MemberRoleChangedDomainEvent>
{
    public Task HandleAsync(MemberRoleChangedDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return bus.PublishAsync(
            new MemberRoleChangedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredAt: clock.GetUtcNow().UtcDateTime,
                HouseholdId: domainEvent.HouseholdId.Value,
                PersonId: domainEvent.PersonId.Value,
                PreviousRole: domainEvent.PreviousRole.ToString(),
                NewRole: domainEvent.NewRole.ToString()),
            ct);
    }
}
