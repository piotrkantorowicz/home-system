namespace Household.UnitTests.Application.EventHandlers;

using Household.Application.EventHandlers;
using Household.Contracts.Events;
using Household.Domain.Events;
using Household.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Messaging;

/// <summary>Unit tests for <c>HouseholdIntegrationEventPublishing</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class HouseholdIntegrationEventPublishingTests
{
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly FakeTimeProvider _clock = TestClock.Create();

    /// <summary><c>HouseholdCreated</c> publishes integration event and with ids.</summary>
    [Fact]
    public async Task HouseholdCreated_PublishesIntegrationEvent_WithIds()
    {
        var householdId = HouseholdId.New();
        var ownerId = PersonId.New();
        var sut = new HouseholdCreatedDomainEventHandler(_bus, _clock);

        await sut.HandleAsync(new HouseholdCreatedDomainEvent(householdId, ownerId), TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<HouseholdCreatedIntegrationEvent>(e =>
                e.HouseholdId == householdId.Value
                && e.OwnerPersonId == ownerId.Value
                && e.EventId != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    /// <summary><c>MemberJoined</c> publishes integration event and with role as string.</summary>
    [Fact]
    public async Task MemberJoined_PublishesIntegrationEvent_WithRoleAsString()
    {
        var householdId = HouseholdId.New();
        var personId = PersonId.New();
        var sut = new MemberJoinedHouseholdDomainEventHandler(_bus, _clock);

        await sut.HandleAsync(
            new MemberJoinedHouseholdDomainEvent(householdId, personId, HouseholdRole.Adult),
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MemberJoinedHouseholdIntegrationEvent>(e =>
                e.HouseholdId == householdId.Value
                && e.PersonId == personId.Value
                && e.Role == "Adult"),
            Arg.Any<CancellationToken>());
    }

    /// <summary><c>MemberLeft</c> publishes integration event.</summary>
    [Fact]
    public async Task MemberLeft_PublishesIntegrationEvent()
    {
        var householdId = HouseholdId.New();
        var personId = PersonId.New();
        var sut = new MemberLeftHouseholdDomainEventHandler(_bus, _clock);

        await sut.HandleAsync(new MemberLeftHouseholdDomainEvent(householdId, personId), TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MemberLeftHouseholdIntegrationEvent>(e =>
                e.HouseholdId == householdId.Value && e.PersonId == personId.Value),
            Arg.Any<CancellationToken>());
    }

    /// <summary><c>MemberRoleChanged</c> publishes integration event and with both roles.</summary>
    [Fact]
    public async Task MemberRoleChanged_PublishesIntegrationEvent_WithBothRoles()
    {
        var householdId = HouseholdId.New();
        var personId = PersonId.New();
        var sut = new MemberRoleChangedDomainEventHandler(_bus, _clock);

        await sut.HandleAsync(
            new MemberRoleChangedDomainEvent(householdId, personId, HouseholdRole.Child, HouseholdRole.Adult),
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MemberRoleChangedIntegrationEvent>(e =>
                e.PreviousRole == "Child" && e.NewRole == "Adult" && e.PersonId == personId.Value),
            Arg.Any<CancellationToken>());
    }
}
