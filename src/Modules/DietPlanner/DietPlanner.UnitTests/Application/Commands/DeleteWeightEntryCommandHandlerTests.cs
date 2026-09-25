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
        var entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 80m, TestClock.UtcNow);
        var latestRemaining = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today.AddDays(-1), 81m, TestClock.UtcNow);
        _weightRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _weightRepo.GetLatestByPersonAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), entry.Id, Arg.Any<CancellationToken>())
            .Returns(latestRemaining);
        var profile = UserProfile.Create(UserProfileId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), null, null, null, 80m, null, null, TestClock.UtcNow);
        _profileRepo.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.HandleAsync(new DeleteWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), entry.Id.Value), TestContext.Current.CancellationToken);

        _weightRepo.Received(1).Delete(entry);
        profile.CurrentWeightKg.ShouldBe(81m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When last entry deleted: <c>HandleAsync</c> sets profile current weight to null.</summary>
    [Fact]
    public async Task HandleAsync_WhenLastEntryDeleted_SetsProfileCurrentWeightToNull()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 80m, TestClock.UtcNow);
        _weightRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        _weightRepo.GetLatestByPersonAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), entry.Id, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        var profile = UserProfile.Create(UserProfileId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), null, null, null, 80m, null, null, TestClock.UtcNow);
        _profileRepo.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.HandleAsync(new DeleteWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), entry.Id.Value), TestContext.Current.CancellationToken);

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
            new DeleteWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), entryId), TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<NotFoundException>();
    }

    /// <summary>When entry belongs to other user: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryBelongsToOtherUser_ThrowsNotFoundException()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("1e5f1a27-4c4e-5b84-b4dd-5227b40c755d"), Today, 80m, TestClock.UtcNow);
        _weightRepo.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var act = () => _sut.HandleAsync(
            new DeleteWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), entry.Id.Value), TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<NotFoundException>();
        _weightRepo.DidNotReceive().Delete(Arg.Any<WeightEntry>());
    }
}
