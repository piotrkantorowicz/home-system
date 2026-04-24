namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005
using DietPlanner.Application.Commands.CompleteMealEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class CompleteMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompleteMealEntryCommandHandler _sut;

    public CompleteMealEntryCommandHandlerTests()
        => _sut = new CompleteMealEntryCommandHandler(_repository, _unitOfWork);

    private static MealEntry NewEntry(string userId = "user-1")
        => MealEntry.Create(MealEntryId.New(), userId, new DateOnly(2026, 1, 1),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null);

    [Fact]
    public async Task HandleAsync_WithOwnedEntry_MarksDoneAndCommits()
    {
        var entry = NewEntry();
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await _sut.HandleAsync(new CompleteMealEntryCommand(entry.Id.Value, "user-1"), CancellationToken.None);

        entry.Status.ShouldBe(MealEntryStatus.Done);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntryMissing_ThrowsNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<MealEntryId>(), Arg.Any<CancellationToken>())
            .Returns((MealEntry?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new CompleteMealEntryCommand(Guid.NewGuid(), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenEntryOwnedByOtherUser_ThrowsNotFoundException()
    {
        var entry = NewEntry("someone-else");
        _repository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new CompleteMealEntryCommand(entry.Id.Value, "user-1"), CancellationToken.None));
    }
}
