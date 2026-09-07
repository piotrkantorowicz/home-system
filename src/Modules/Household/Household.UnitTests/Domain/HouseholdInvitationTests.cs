using Household.Domain.Aggregates;
using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;

namespace Household.UnitTests.Domain;

public sealed class HouseholdInvitationTests
{
    private static HouseholdInvitation NewInvitation(HouseholdRole role = HouseholdRole.Adult)
        => HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            HouseholdId.New(),
            PersonEmail.Create("invitee@example.com"),
            role,
            PersonId.New());

    [Fact]
    public void Create_IsPending_ExpiresIn30Days_AndRaisesCreatedEvent()
    {
        var invitation = NewInvitation();

        invitation.IsPending.ShouldBeTrue();
        invitation.ExpiresAt.ShouldBe(invitation.CreatedAt.Add(HouseholdInvitation.Lifetime), tolerance: TimeSpan.FromSeconds(1));
        invitation.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<HouseholdInvitationCreatedDomainEvent>();
    }

    [Fact]
    public void Create_ForOwnerRole_Throws()
        => Should.Throw<HouseholdDomainException>(() => NewInvitation(HouseholdRole.Owner));

    [Fact]
    public void Accept_WhenPendingAndFresh_MarksAccepted()
    {
        var invitation = NewInvitation();

        invitation.Accept(DateTime.UtcNow);

        invitation.Status.ShouldBe(InvitationStatus.Accepted);
        invitation.ResolvedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Accept_WhenExpired_MarksExpired_AndThrows()
    {
        var invitation = NewInvitation();

        Should.Throw<HouseholdDomainException>(
            () => invitation.Accept(invitation.ExpiresAt.AddSeconds(1)));

        invitation.Status.ShouldBe(InvitationStatus.Expired);
    }

    [Fact]
    public void Revoke_WhenPending_MarksRevoked()
    {
        var invitation = NewInvitation();

        invitation.Revoke(DateTime.UtcNow);

        invitation.Status.ShouldBe(InvitationStatus.Revoked);
    }

    [Fact]
    public void Accept_AfterRevoke_Throws()
    {
        var invitation = NewInvitation();
        invitation.Revoke(DateTime.UtcNow);

        Should.Throw<HouseholdDomainException>(() => invitation.Accept(DateTime.UtcNow));
    }
}
