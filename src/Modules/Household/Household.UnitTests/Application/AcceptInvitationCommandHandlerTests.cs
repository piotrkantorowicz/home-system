namespace Household.UnitTests.Application;

using Household.Application.Commands.AcceptInvitation;
using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>Unit tests for <c>AcceptInvitationCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class AcceptInvitationCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdInvitationRepository _invitations = Substitute.For<IHouseholdInvitationRepository>();
    private readonly IHouseholdUnitOfWork _uow = Substitute.For<IHouseholdUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly AcceptInvitationCommandHandler _sut;

    private readonly Person _invitee = Person.RegisterFromLogin(
        PersonId.New(), "auth|invitee", "Invitee", PersonEmail.Create("invitee@example.com"), null, TestClock.UtcNow);
    private readonly HouseholdAggregate _household;
    private readonly HouseholdInvitation _invitation;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public AcceptInvitationCommandHandlerTests()
    {
        _household = HouseholdAggregate.Create(HouseholdId.New(), "Home", PersonId.New(), TestClock.UtcNow);
        _invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(), _household.Id, PersonEmail.Create("invitee@example.com"),
            targetPersonId: null, HouseholdRole.Adult, PersonId.New(), TestClock.UtcNow, nickname: "Gram");

        _sut = new AcceptInvitationCommandHandler(
            new HouseholdAccessService(_persons, _households), _invitations, _households, _uow, _clock);

        _persons.GetByAuthSubjectAsync("auth|invitee", Arg.Any<CancellationToken>()).Returns(_invitee);
        _invitations.GetByIdAsync(_invitation.Id, Arg.Any<CancellationToken>()).Returns(_invitation);
        _households.GetByIdAsync(_household.Id, Arg.Any<CancellationToken>()).Returns(_household);
    }

    private AcceptInvitationCommand Command() => new("auth|invitee", _invitation.Id.Value);

    /// <summary>When pending and addressed to the caller: <c>Handle</c> adds the member with the invitation's role and nickname, accepts, and commits.</summary>
    [Fact]
    public async Task Handle_WhenPendingAndAddressedToCaller_AddsMember_Accepts_AndCommits()
    {
        await _sut.HandleAsync(Command(), TestContext.Current.CancellationToken);

        _household.HasMember(_invitee.Id).ShouldBeTrue();
        _household.RoleOf(_invitee.Id).ShouldBe(HouseholdRole.Adult);
        _invitation.Status.ShouldBe(InvitationStatus.Accepted);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the invitation is addressed to a different email: <c>Handle</c> throws forbidden.</summary>
    [Fact]
    public async Task Handle_WhenAddressedToADifferentEmail_Throws()
    {
        var stranger = Person.RegisterFromLogin(
            PersonId.New(), "auth|stranger", "Stranger", PersonEmail.Create("stranger@example.com"), null, TestClock.UtcNow);
        _persons.GetByAuthSubjectAsync("auth|stranger", Arg.Any<CancellationToken>()).Returns(stranger);

        await Should.ThrowAsync<ForbiddenException>(() => _sut.HandleAsync(
            new AcceptInvitationCommand("auth|stranger", _invitation.Id.Value), TestContext.Current.CancellationToken));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When the caller already belongs to a household: <c>Handle</c> throws.</summary>
    [Fact]
    public async Task Handle_WhenCallerAlreadyBelongsToAHousehold_Throws()
    {
        _households.GetByMemberPersonIdAsync(_invitee.Id, Arg.Any<CancellationToken>())
            .Returns(HouseholdAggregate.Create(HouseholdId.New(), "Other", _invitee.Id, TestClock.UtcNow));

        await Should.ThrowAsync<HouseholdDomainException>(() =>
            _sut.HandleAsync(Command(), TestContext.Current.CancellationToken));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When expired: <c>Handle</c> marks the invitation expired, commits that, and throws.</summary>
    [Fact]
    public async Task Handle_WhenExpired_MarksExpired_Commits_AndThrows()
    {
        _clock.SetUtcNow(TestClock.Now.Add(HouseholdInvitation.Lifetime).AddSeconds(1));

        await Should.ThrowAsync<HouseholdDomainException>(() =>
            _sut.HandleAsync(Command(), TestContext.Current.CancellationToken));

        _invitation.Status.ShouldBe(InvitationStatus.Expired);
        _household.HasMember(_invitee.Id).ShouldBeFalse();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When no invitation exists for the id: <c>Handle</c> throws not found.</summary>
    [Fact]
    public async Task Handle_WhenInvitationDoesNotExist_Throws()
    {
        _invitations.GetByIdAsync(Arg.Any<HouseholdInvitationId>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdInvitation?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(Command(), TestContext.Current.CancellationToken));
    }
}
