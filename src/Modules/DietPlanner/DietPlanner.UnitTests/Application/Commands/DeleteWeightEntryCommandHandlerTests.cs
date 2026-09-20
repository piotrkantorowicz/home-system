namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.DeleteWeightEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>DeleteWeightEntryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class DeleteWeightEntryCommandHandlerTests
{
    private static readonly DateOnly Today = TestClock.Today;

    private readonly IWeightEntryRepository _weightRepo = Substitute.For<IWeightEntryRepository>();
    private readonly IUserProfileRepository _profileRepo = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly DeleteWeightEntryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public DeleteWeightEntryCommandHandlerTests()
        => _sut = new DeleteWeightEntryCommandHandler(_weightRepo, _profileRepo, _unitOfWork, _clock);

    /// <summary><c>HandleAsync</c> deletes entry and recomputes profile current weight.</summary>
    [Fact]
    public async Task HandleAsync_DeletesEntryAndRecomputesProfileCurrentWeight()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 80m, TestClock.UtcNow);
        var latestRemaining = WeightEntry.Create(WeightEntryId.New(), "user-1", Today.AddDays(-1), 81m, TestClock.UtcNow);
        _weightRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _weightRepo.GetLatestByUserAsync("user-1", entry.Id, Arg.Any<CancellationToken>())
            .Returns(latestRemaining);
        var profile = UserProfile.Create(UserProfileId.New(), "user-1", null, null, null, 80m, null, null, TestClock.UtcNow);
        _profileRepo.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.HandleAsync(new DeleteWeightEntryCommand("user-1", entry.Id.Value), TestContext.Current.CancellationToken);

        _weightRepo.Received(1).Delete(entry);
        profile.CurrentWeightKg.ShouldBe(81m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When last entry deleted: <c>HandleAsync</c> sets profile current weight to null.</summary>
    [Fact]
    public async Task HandleAsync_WhenLastEntryDeleted_SetsProfileCurrentWeightToNull()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 80m, TestClock.UtcNow);
        _weightRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _weightRepo.GetLatestByUserAsync("user-1", entry.Id, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        var profile = UserProfile.Create(UserProfileId.New(), "user-1", null, null, null, 80m, null, null, TestClock.UtcNow);
        _profileRepo.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.HandleAsync(new DeleteWeightEntryCommand("user-1", entry.Id.Value), TestContext.Current.CancellationToken);

        profile.CurrentWeightKg.ShouldBeNull();
    }

    /// <summary>When entry not found: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryNotFound_ThrowsNotFoundException()
    {
        var entryId = Guid.NewGuid();
        _weightRepo.GetByIdAsync(WeightEntryId.From(entryId), Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);

        var act = () => _sut.HandleAsync(
            new DeleteWeightEntryCommand("user-1", entryId), TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<NotFoundException>();
    }

    /// <summary>When entry belongs to other user: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryBelongsToOtherUser_ThrowsNotFoundException()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), "user-2", Today, 80m, TestClock.UtcNow);
        _weightRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var act = () => _sut.HandleAsync(
            new DeleteWeightEntryCommand("user-1", entry.Id.Value), TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<NotFoundException>();
        _weightRepo.DidNotReceive().Delete(Arg.Any<WeightEntry>());
    }
}
