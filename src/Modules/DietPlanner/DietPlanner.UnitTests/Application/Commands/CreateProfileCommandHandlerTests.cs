namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.CreateProfile;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>CreateProfileCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class CreateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly CreateProfileCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public CreateProfileCommandHandlerTests()
        => _sut = new CreateProfileCommandHandler(_repository, _unitOfWork, _clock);

    /// <summary>With valid command: <c>HandleAsync</c> adds profile and commits.</summary>
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
                p.CurrentWeightKg == 80m &&
                p.CreatedAt == _clock.GetUtcNow().UtcDateTime),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>With all nullable fields null: <c>HandleAsync</c> adds profile and commits.</summary>
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
