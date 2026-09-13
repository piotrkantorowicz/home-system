namespace Household.UnitTests.Application.EventHandlers;

using Household.Application.EventHandlers;
using Household.Contracts.Events;
using Household.Domain.Events;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

public sealed class HouseholdIntegrationEventPublishingTests
{
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();

    [Fact]
    public async Task HouseholdCreated_PublishesIntegrationEvent_WithIds()
    {
        var householdId = HouseholdId.New();
        var ownerId = PersonId.New();
        var sut = new HouseholdCreatedDomainEventHandler(_bus);

        await sut.HandleAsync(new HouseholdCreatedDomainEvent(householdId, ownerId), CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<HouseholdCreatedIntegrationEvent>(e =>
                e.HouseholdId == householdId.Value
                && e.OwnerPersonId == ownerId.Value
                && e.EventId != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MemberJoined_PublishesIntegrationEvent_WithRoleAsString()
    {
        var householdId = HouseholdId.New();
        var personId = PersonId.New();
        var sut = new MemberJoinedHouseholdDomainEventHandler(_bus);

        await sut.HandleAsync(
            new MemberJoinedHouseholdDomainEvent(householdId, personId, HouseholdRole.Adult),
            CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MemberJoinedHouseholdIntegrationEvent>(e =>
                e.HouseholdId == householdId.Value
                && e.PersonId == personId.Value
                && e.Role == "Adult"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MemberLeft_PublishesIntegrationEvent()
    {
        var householdId = HouseholdId.New();
        var personId = PersonId.New();
        var sut = new MemberLeftHouseholdDomainEventHandler(_bus);

        await sut.HandleAsync(new MemberLeftHouseholdDomainEvent(householdId, personId), CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MemberLeftHouseholdIntegrationEvent>(e =>
                e.HouseholdId == householdId.Value && e.PersonId == personId.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MemberRoleChanged_PublishesIntegrationEvent_WithBothRoles()
    {
        var householdId = HouseholdId.New();
        var personId = PersonId.New();
        var sut = new MemberRoleChangedDomainEventHandler(_bus);

        await sut.HandleAsync(
            new MemberRoleChangedDomainEvent(householdId, personId, HouseholdRole.Child, HouseholdRole.Adult),
            CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MemberRoleChangedIntegrationEvent>(e =>
                e.PreviousRole == "Child" && e.NewRole == "Adult" && e.PersonId == personId.Value),
            Arg.Any<CancellationToken>());
    }
}
