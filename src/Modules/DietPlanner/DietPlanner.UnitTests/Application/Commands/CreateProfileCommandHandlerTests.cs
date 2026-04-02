namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.CreateProfile;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Domain;

public sealed class CreateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProfileCommandHandler _sut;

    public CreateProfileCommandHandlerTests()
        => _sut = new CreateProfileCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsProfileAndCommits()
    {
        var command = new CreateProfileCommand(
            "user-1",
            new DateOnly(1990, 5, 15),
            "Male",
            180m,
            80m,
            75m,
            "ModeratelyActive");

        Guid id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<UserProfile>(p =>
                p.UserId == "user-1" &&
                p.HeightCm == 180m &&
                p.CurrentWeightKg == 80m),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithAllNullableFieldsNull_AddsProfileAndCommits()
    {
        var command = new CreateProfileCommand("user-1", null, null, null, null, null, null);

        Guid id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<UserProfile>(p => p.UserId == "user-1"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
