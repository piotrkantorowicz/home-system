namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.UpdateProfile;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProfileCommandHandler _sut;

    public UpdateProfileCommandHandlerTests()
        => _sut = new UpdateProfileCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WhenProfileExists_UpdatesAndCommits()
    {
        UserProfile existing = UserProfile.Create(
            UserProfileId.New(), "user-1",
            new DateOnly(1990, 5, 15), Gender.Male,
            180m, 80m, 75m, ActivityLevel.Sedentary);

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateProfileCommand(
            "user-1",
            new DateOnly(1990, 5, 15),
            "Female",
            175m,
            70m,
            65m,
            "VeryActive");

        await _sut.HandleAsync(command, CancellationToken.None);

        _repository.Received(1).Update(Arg.Is<UserProfile>(p => p.HeightCm == 175m));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _repository.GetByUserIdAsync("unknown-user", Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var command = new UpdateProfileCommand("unknown-user", null, null, null, null, null, null);

        await Should.ThrowAsync<NotFoundException>(() => _sut.HandleAsync(command, CancellationToken.None));
        _repository.DidNotReceive().Update(Arg.Any<UserProfile>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
