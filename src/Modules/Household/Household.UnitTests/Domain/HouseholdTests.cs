namespace Household.UnitTests.Domain;

using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>Unit tests for <c>Household</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class HouseholdTests
{
    private static PersonId NewPerson() => PersonId.New();

    /// <summary><c>Create</c> starts with one owner and raises created and joined events.</summary>
    [Fact]
    public void Create_StartsWithOneOwner_AndRaisesCreatedAndJoinedEvents()
    {
        var owner = NewPerson();

        var household = HouseholdAggregate.Create(HouseholdId.New(), "  The Kowalskis  ", owner, TestClock.UtcNow);

        household.Name.ShouldBe("The Kowalskis");
        household.Members.ShouldHaveSingleItem();
        household.RoleOf(owner).ShouldBe(HouseholdRole.Owner);
        household.DomainEvents.Select(e => e.GetType()).ShouldBe(
            new[] { typeof(HouseholdCreatedDomainEvent), typeof(MemberJoinedHouseholdDomainEvent) });
    }

    /// <summary>With blank name: <c>Create</c> throws.</summary>
    [Fact]
    public void Create_WithBlankName_Throws()
        => Should.Throw<HouseholdDomainException>(
            () => HouseholdAggregate.Create(HouseholdId.New(), "   ", NewPerson(), TestClock.UtcNow));

    /// <summary><c>AddMember</c> adds with role and raises joined.</summary>
    [Fact]
    public void AddMember_AddsWithRole_AndRaisesJoined()
    {
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", NewPerson(), TestClock.UtcNow);
        household.ClearDomainEvents();
        var adam = NewPerson();

        household.AddMember(adam, HouseholdRole.Child, TestClock.UtcNow, "Adam");

        household.RoleOf(adam).ShouldBe(HouseholdRole.Child);
        household.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MemberJoinedHouseholdDomainEvent>();
    }

    /// <summary>When already a member: <c>AddMember</c> throws.</summary>
    [Fact]
    public void AddMember_WhenAlreadyAMember_Throws()
    {
        var owner = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner, TestClock.UtcNow);

        Should.Throw<HouseholdDomainException>(() => household.AddMember(owner, HouseholdRole.Adult, TestClock.UtcNow));
    }

    /// <summary>The only owner: <c>RemoveMember</c> throws.</summary>
    [Fact]
    public void RemoveMember_TheOnlyOwner_Throws()
    {
        var owner = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner, TestClock.UtcNow);

        Should.Throw<HouseholdDomainException>(() => household.RemoveMember(owner, TestClock.UtcNow))
            .Message.ShouldContain("at least one owner");
    }

    /// <summary>A non owner: <c>RemoveMember</c> removes and raises left.</summary>
    [Fact]
    public void RemoveMember_ANonOwner_RemovesAndRaisesLeft()
    {
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", NewPerson(), TestClock.UtcNow);
        var guest = NewPerson();
        household.AddMember(guest, HouseholdRole.Guest, TestClock.UtcNow);
        household.ClearDomainEvents();

        household.RemoveMember(guest, TestClock.UtcNow);

        household.HasMember(guest).ShouldBeFalse();
        household.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MemberLeftHouseholdDomainEvent>();
    }

    /// <summary>Demoting the only owner: <c>ChangeMemberRole</c> throws.</summary>
    [Fact]
    public void ChangeMemberRole_DemotingTheOnlyOwner_Throws()
    {
        var owner = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner, TestClock.UtcNow);

        Should.Throw<HouseholdDomainException>(
            () => household.ChangeMemberRole(owner, HouseholdRole.Adult, TestClock.UtcNow));
    }

    /// <summary>With a second owner present: <c>ChangeMemberRole</c> allows demotion and raises role changed.</summary>
    [Fact]
    public void ChangeMemberRole_WithASecondOwnerPresent_AllowsDemotion_AndRaisesRoleChanged()
    {
        var owner1 = NewPerson();
        var owner2 = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner1, TestClock.UtcNow);
        household.AddMember(owner2, HouseholdRole.Owner, TestClock.UtcNow);
        household.ClearDomainEvents();

        household.ChangeMemberRole(owner1, HouseholdRole.Adult, TestClock.UtcNow);

        household.RoleOf(owner1).ShouldBe(HouseholdRole.Adult);
        var evt = household.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MemberRoleChangedDomainEvent>();
        evt.PreviousRole.ShouldBe(HouseholdRole.Owner);
        evt.NewRole.ShouldBe(HouseholdRole.Adult);
    }

    /// <summary>When unchanged: <c>Rename</c> does not stamp updated at.</summary>
    [Fact]
    public void Rename_WhenUnchanged_DoesNotStampUpdatedAt()
    {
        var household = HouseholdAggregate.Create(HouseholdId.New(), "Home", NewPerson(), TestClock.UtcNow);

        household.Rename("Home", TestClock.UtcNow);

        household.UpdatedAt.ShouldBeNull();
    }
}
