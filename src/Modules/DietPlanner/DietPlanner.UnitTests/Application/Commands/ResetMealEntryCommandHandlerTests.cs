namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.ResetMealEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>ResetMealEntryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class ResetMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResetMealEntryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public ResetMealEntryCommandHandlerTests()
        => _sut = new ResetMealEntryCommandHandler(_repository, _unitOfWork, TestHouseholds.Solo());

    /// <summary><c>HandleAsync</c> resets entry and commits.</summary>
    [Fact]
    public async Task HandleAsync_ResetsEntryAndCommits()
    {
        var entry = MealEntry.Create(MealEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null, TestClock.UtcNow);
        entry.ApplyOverride(RecipeId.New(), []);

        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await _sut.HandleAsync(new ResetMealEntryCommand(entry.Id.Value, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), "user-1"), TestContext.Current.CancellationToken);

        entry.Status.ShouldBe(MealEntryStatus.Planned);
        entry.ActualRecipeId.ShouldBeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When entry not owned: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryNotOwned_ThrowsNotFoundException()
    {
        var entry = MealEntry.Create(MealEntryId.New(), Guid.Parse("1e5f1a27-4c4e-5b84-b4dd-5227b40c755d"), new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null, TestClock.UtcNow);
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new ResetMealEntryCommand(entry.Id.Value, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), "user-1"), TestContext.Current.CancellationToken));
    }
}
