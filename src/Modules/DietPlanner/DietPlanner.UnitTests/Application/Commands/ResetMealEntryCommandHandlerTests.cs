namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005
using DietPlanner.Application.Commands.ResetMealEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class ResetMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResetMealEntryCommandHandler _sut;

    public ResetMealEntryCommandHandlerTests()
        => _sut = new ResetMealEntryCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_ResetsEntryAndCommits()
    {
        var entry = MealEntry.Create(MealEntryId.New(), "user-1", new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null);
        entry.ApplyOverride(RecipeId.New(), []);

        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await _sut.HandleAsync(new ResetMealEntryCommand(entry.Id.Value, "user-1"), CancellationToken.None);

        entry.Status.ShouldBe(MealEntryStatus.Planned);
        entry.ActualRecipeId.ShouldBeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntryNotOwned_ThrowsNotFoundException()
    {
        var entry = MealEntry.Create(MealEntryId.New(), "other-user", new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null);
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new ResetMealEntryCommand(entry.Id.Value, "user-1"), CancellationToken.None));
    }
}
