namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.CompleteMealEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>CompleteMealEntryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class CompleteMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompleteMealEntryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public CompleteMealEntryCommandHandlerTests()
        => _sut = new CompleteMealEntryCommandHandler(_repository, _unitOfWork, TestHouseholds.Solo());

    private static MealEntry NewEntry(Guid personId = default)
        => MealEntry.Create(MealEntryId.New(), personId == Guid.Empty ? Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb") : personId, new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null, TestClock.UtcNow);

    /// <summary>With owned entry: <c>HandleAsync</c> marks done and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithOwnedEntry_MarksDoneAndCommits()
    {
        var entry = NewEntry();
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await _sut.HandleAsync(new CompleteMealEntryCommand(entry.Id.Value, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), "user-1"), TestContext.Current.CancellationToken);

        entry.Status.ShouldBe(MealEntryStatus.Done);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When entry missing: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryMissing_ThrowsNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<MealEntryId>(), Arg.Any<CancellationToken>())
            .Returns((MealEntry?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new CompleteMealEntryCommand(Guid.NewGuid(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), "user-1"), TestContext.Current.CancellationToken));
    }

    /// <summary>When entry owned by other user: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryOwnedByOtherUser_ThrowsNotFoundException()
    {
        var entry = NewEntry(Guid.Parse("7908be49-47dc-48f4-872d-4d3dfd445590"));
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new CompleteMealEntryCommand(entry.Id.Value, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), "user-1"), TestContext.Current.CancellationToken));
    }
}
