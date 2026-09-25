namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.UpdateProfile;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>UpdateProfileCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly UpdateProfileCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public UpdateProfileCommandHandlerTests()
        => _sut = new UpdateProfileCommandHandler(_repository, _unitOfWork, _clock);

    /// <summary>When profile exists: <c>HandleAsync</c> updates and commits.</summary>
    [Fact]
    public async Task HandleAsync_WhenProfileExists_UpdatesAndCommits()
    {
        UserProfile existing = UserProfile.Create(
            UserProfileId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            new DateOnly(1990, 5, 15), Gender.Male,
            180m, 80m, 75m, ActivityLevel.Sedentary, TestClock.UtcNow);

        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateProfileCommand(
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            new DateOnly(1990, 5, 15),
            "Female",
            175m,
            70m,
            65m,
            "VeryActive");

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        _repository.Received(1).Update(Arg.Is<UserProfile>(p => p.HeightCm == 175m));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When profile not found: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _repository.GetByPersonIdAsync(Guid.Parse("764ae4ad-7805-5482-b116-3e6d4e118a1a"), Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var command = new UpdateProfileCommand(Guid.Parse("764ae4ad-7805-5482-b116-3e6d4e118a1a"), null, null, null, null, null, null);

        await Should.ThrowAsync<NotFoundException>(() => _sut.HandleAsync(command, TestContext.Current.CancellationToken));
        _repository.DidNotReceive().Update(Arg.Any<UserProfile>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
