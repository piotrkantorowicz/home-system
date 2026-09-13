namespace Household.UnitTests.Application;

using Household.Application.Commands.SyncCurrentPerson;
using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;

public sealed class SyncCurrentPersonCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdInvitationRepository _invitations = Substitute.For<IHouseholdInvitationRepository>();
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdUnitOfWork _unitOfWork = Substitute.For<IHouseholdUnitOfWork>();
    private readonly SyncCurrentPersonCommandHandler _sut;

    public SyncCurrentPersonCommandHandlerTests()
        => _sut = new SyncCurrentPersonCommandHandler(
            _persons,
            new InvitationResolver(_invitations, _households),
            _unitOfWork);

    [Fact]
    public async Task Handle_WhenNoPersonForSubject_RegistersAndCommits()
    {
        _persons.GetByAuthSubjectAsync("auth|new", Arg.Any<CancellationToken>())
            .Returns((Person?)null);

        var result = await _sut.HandleAsync(
            new SyncCurrentPersonCommand("auth|new", "New User", "new@x.com", null),
            CancellationToken.None);

        result.ShouldNotBe(Guid.Empty);
        await _persons.Received(1).AddAsync(
            Arg.Is<Person>(p => p.AuthSubject == "auth|new" && p.Email!.Value == "new@x.com"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPersonExists_RefreshesProfileAndCommits_WithoutAdding()
    {
        var existing = Person.RegisterFromLogin(PersonId.New(), "auth|1", "Old", null, null);
        _persons.GetByAuthSubjectAsync("auth|1", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.HandleAsync(
            new SyncCurrentPersonCommand("auth|1", "New Name", "n@x.com", "pic"),
            CancellationToken.None);

        result.ShouldBe(existing.Id.Value);
        existing.DisplayName.ShouldBe("New Name");
        existing.Email!.Value.ShouldBe("n@x.com");
        await _persons.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLoginEmailMatchesAManagedPerson_LinksItInsteadOfCreating()
    {
        var managed = Person.CreateManaged(PersonId.New(), "Kiddo", PersonEmail.Create("kiddo@x.com"));
        _persons.GetByAuthSubjectAsync("auth|kiddo", Arg.Any<CancellationToken>()).Returns((Person?)null);
        _persons.GetByEmailAsync(
            Arg.Is<PersonEmail>(e => e.Value == "kiddo@x.com"), Arg.Any<CancellationToken>())
            .Returns(managed);

        var result = await _sut.HandleAsync(
            new SyncCurrentPersonCommand("auth|kiddo", "Kiddo Grown", "kiddo@x.com", null),
            CancellationToken.None);

        result.ShouldBe(managed.Id.Value);
        managed.AuthSubject.ShouldBe("auth|kiddo");
        managed.IsManaged.ShouldBeFalse();
        await _persons.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailMatchesAnAlreadyLinkedPerson_RegistersANewPerson()
    {
        var linked = Person.RegisterFromLogin(PersonId.New(), "auth|other", "Other", PersonEmail.Create("shared@x.com"), null);
        _persons.GetByAuthSubjectAsync("auth|fresh", Arg.Any<CancellationToken>()).Returns((Person?)null);
        _persons.GetByEmailAsync(Arg.Any<PersonEmail>(), Arg.Any<CancellationToken>()).Returns(linked);

        await _sut.HandleAsync(
            new SyncCurrentPersonCommand("auth|fresh", "Fresh", "shared@x.com", null),
            CancellationToken.None);

        await _persons.Received(1).AddAsync(
            Arg.Is<Person>(p => p.AuthSubject == "auth|fresh"), Arg.Any<CancellationToken>());
    }
}
