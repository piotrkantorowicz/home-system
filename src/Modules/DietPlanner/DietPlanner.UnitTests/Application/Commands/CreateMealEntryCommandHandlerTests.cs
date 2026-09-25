namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateMealEntry;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>CreateMealEntryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class CreateMealEntryCommandHandlerTests
{
    private readonly IMealEntryRepository _repository = Substitute.For<IMealEntryRepository>();
    private readonly IMealScheduleConfigRepository _scheduleRepository =
        Substitute.For<IMealScheduleConfigRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly CreateMealEntryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public CreateMealEntryCommandHandlerTests()
        => _sut = new CreateMealEntryCommandHandler(_repository, _scheduleRepository, _unitOfWork, _clock);

    /// <summary>With valid command: <c>HandleAsync</c> adds meal entry and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsMealEntryAndCommits()
    {
        var schedule = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [("Breakfast", new TimeOnly(7, 0))],
            TestClock.UtcNow);
        var slot = schedule.Slots.Single();
        _scheduleRepository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(schedule);

        var recipeId = Guid.NewGuid();
        var date = new DateOnly(2024, 3, 15);
        var command = new CreateMealEntryCommand(
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), date, slot.Id.Value, recipeId, 1.5m, null, null, null);

        var id = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<MealEntry>(m => m.PersonId == Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb") && m.MealSlotId == slot.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When slot does not exist: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenSlotDoesNotExist_ThrowsNotFoundException()
    {
        var schedule = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [("Breakfast", new TimeOnly(7, 0))],
            TestClock.UtcNow);
        _scheduleRepository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(schedule);

        var command = new CreateMealEntryCommand(
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), new DateOnly(2024, 3, 15), Guid.NewGuid(), Guid.NewGuid(), 1m, null, null, null);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(command, TestContext.Current.CancellationToken));
    }

    /// <summary>When schedule missing: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenScheduleMissing_ThrowsNotFoundException()
    {
        _scheduleRepository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>())
            .Returns((MealScheduleConfig?)null);

        var command = new CreateMealEntryCommand(
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), new DateOnly(2024, 3, 15), Guid.NewGuid(), Guid.NewGuid(), 1m, null, null, null);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.HandleAsync(command, TestContext.Current.CancellationToken));
    }
}
