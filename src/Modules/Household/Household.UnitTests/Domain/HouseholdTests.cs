namespace Household.UnitTests.Domain;

using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

public sealed class HouseholdTests
{
    private static PersonId NewPerson() => PersonId.New();

    [Fact]
    public void Create_StartsWithOneOwner_AndRaisesCreatedAndJoinedEvents()
    {
        var owner = NewPerson();

        var household = HouseholdAggregate.Create(HouseholdId.New(), "  The Kowalskis  ", owner);

        household.Name.ShouldBe("The Kowalskis");
        household.Members.ShouldHaveSingleItem();
        household.RoleOf(owner).ShouldBe(HouseholdRole.Owner);
        household.DomainEvents.Select(e => e.GetType()).ShouldBe(
            new[] { typeof(HouseholdCreatedDomainEvent), typeof(MemberJoinedHouseholdDomainEvent) });
    }

    [Fact]
    public void Create_WithBlankName_Throws()
        => Should.Throw<HouseholdDomainException>(
            () => HouseholdAggregate.Create(HouseholdId.New(), "   ", NewPerson()));

    [Fact]
    public void AddMember_AddsWithRole_AndRaisesJoined()
    {
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", NewPerson());
        household.ClearDomainEvents();
        var adam = NewPerson();

        household.AddMember(adam, HouseholdRole.Child, "Adam");

        household.RoleOf(adam).ShouldBe(HouseholdRole.Child);
        household.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MemberJoinedHouseholdDomainEvent>();
    }

    [Fact]
    public void AddMember_WhenAlreadyAMember_Throws()
    {
        var owner = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner);

        Should.Throw<HouseholdDomainException>(() => household.AddMember(owner, HouseholdRole.Adult));
    }

    [Fact]
    public void RemoveMember_TheOnlyOwner_Throws()
    {
        var owner = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner);

        Should.Throw<HouseholdDomainException>(() => household.RemoveMember(owner))
            .Message.ShouldContain("at least one owner");
    }

    [Fact]
    public void RemoveMember_ANonOwner_RemovesAndRaisesLeft()
    {
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", NewPerson());
        var guest = NewPerson();
        household.AddMember(guest, HouseholdRole.Guest);
        household.ClearDomainEvents();

        household.RemoveMember(guest);

        household.HasMember(guest).ShouldBeFalse();
        household.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MemberLeftHouseholdDomainEvent>();
    }

    [Fact]
    public void ChangeMemberRole_DemotingTheOnlyOwner_Throws()
    {
        var owner = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner);

        Should.Throw<HouseholdDomainException>(
            () => household.ChangeMemberRole(owner, HouseholdRole.Adult));
    }

    [Fact]
    public void ChangeMemberRole_WithASecondOwnerPresent_AllowsDemotion_AndRaisesRoleChanged()
    {
        var owner1 = NewPerson();
        var owner2 = NewPerson();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "H", owner1);
        household.AddMember(owner2, HouseholdRole.Owner);
        household.ClearDomainEvents();

        household.ChangeMemberRole(owner1, HouseholdRole.Adult);

        household.RoleOf(owner1).ShouldBe(HouseholdRole.Adult);
        var evt = household.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MemberRoleChangedDomainEvent>();
        evt.PreviousRole.ShouldBe(HouseholdRole.Owner);
        evt.NewRole.ShouldBe(HouseholdRole.Adult);
    }

    [Fact]
    public void Rename_WhenUnchanged_DoesNotStampUpdatedAt()
    {
        var household = HouseholdAggregate.Create(HouseholdId.New(), "Home", NewPerson());

        household.Rename("Home");

        household.UpdatedAt.ShouldBeNull();
    }
}
