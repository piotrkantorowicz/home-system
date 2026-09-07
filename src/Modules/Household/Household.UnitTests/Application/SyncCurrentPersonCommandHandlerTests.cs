namespace Household.UnitTests.Application;

using global::Household.Application.Commands.SyncCurrentPerson;
using global::Household.Domain.Abstractions;
using global::Household.Domain.Aggregates;
using global::Household.Domain.ValueObjects;

public sealed class SyncCurrentPersonCommandHandlerTests
{
    private readonly IPersonRepository _persons = Substitute.For<IPersonRepository>();
    private readonly IHouseholdUnitOfWork _unitOfWork = Substitute.For<IHouseholdUnitOfWork>();
    private readonly SyncCurrentPersonCommandHandler _sut;

    public SyncCurrentPersonCommandHandlerTests()
        => _sut = new SyncCurrentPersonCommandHandler(_persons, _unitOfWork);

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
}
