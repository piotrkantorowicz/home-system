namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class CreateMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IMealScheduleConfigRepository _scheduleRepository =
        Substitute.For<IMealScheduleConfigRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateMealEntryCommandHandler _sut;

    public CreateMealEntryCommandHandlerTests()
        => _sut = new CreateMealEntryCommandHandler(_repository, _scheduleRepository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsMealEntryAndCommits()
    {
        var schedule = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("Breakfast", new TimeOnly(7, 0))]);
        var slot = schedule.Slots.Single();
        _scheduleRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(schedule);

        var recipeId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 15);
        var command = new CreateMealEntryCommand(
            "user-1", date, slot.Id.Value, recipeId, 1.5m, null, null, null);

        var id = await _sut.HandleAsync(command, CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<MealEntry>(m => m.UserId == "user-1" && m.MealSlotId == slot.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenSlotDoesNotExist_ThrowsNotFoundException()
    {
        var schedule = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("Breakfast", new TimeOnly(7, 0))]);
        _scheduleRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(schedule);

        var command = new CreateMealEntryCommand(
            "user-1", new DateOnly(2024, 3, 15), Guid.NewGuid(), Guid.NewGuid(), 1m, null, null, null);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenScheduleMissing_ThrowsNotFoundException()
    {
        _scheduleRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((MealScheduleConfig?)null);

        var command = new CreateMealEntryCommand(
            "user-1", new DateOnly(2024, 3, 15), Guid.NewGuid(), Guid.NewGuid(), 1m, null, null, null);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(command, CancellationToken.None));
    }
}
