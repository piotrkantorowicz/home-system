namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.UpdateMealSchedule;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>UpdateMealScheduleCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class UpdateMealScheduleCommandHandlerTests
{
    private readonly IMealScheduleConfigRepository _repository =
        Substitute.For<IMealScheduleConfigRepository>();
    private readonly IMealEntryRepository _mealEntryRepository =
        Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly UpdateMealScheduleCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public UpdateMealScheduleCommandHandlerTests()
        => _sut = new UpdateMealScheduleCommandHandler(_repository, _mealEntryRepository, _unitOfWork, _clock, TestHouseholds.Solo());

    private static UpdateMealScheduleCommand NewCommand(params MealSlotInput[] slots)
        => new(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), slots, "user-1");

    /// <summary>When no existing config: <c>HandleAsync</c> creates new and commits.</summary>
    [Fact]
    public async Task HandleAsync_WhenNoExistingConfig_CreatesNewAndCommits()
    {
        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>())
            .Returns((MealScheduleConfig?)null);

        var command = NewCommand(
            new MealSlotInput(null, "Breakfast", "07:00"),
            new MealSlotInput(null, "Lunch", "12:00"),
            new MealSlotInput(null, "Dinner", "18:00"));

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        await _repository.Received(1).AddAsync(
            Arg.Is<MealScheduleConfig>(c => c.PersonId == Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb") && c.Slots.Count == 3),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary><c>HandleAsync</c> preserves ids for kept slots.</summary>
    [Fact]
    public async Task HandleAsync_PreservesIdsForKeptSlots()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [("Breakfast", new TimeOnly(7, 0)), ("Lunch", new TimeOnly(12, 0))],
            TestClock.UtcNow);
        var existingSlots = existing.Slots.OrderBy(s => s.SortOrder).ToList();

        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(existing);
        _mealEntryRepository.AnyForSlotAsync(Arg.Any<MealSlotId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = NewCommand(
            new MealSlotInput(existingSlots[0].Id.Value, "Brunch", "10:00"),
            new MealSlotInput(existingSlots[1].Id.Value, "Lunch", "12:00"));

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        existing.Slots.Single(s => s.Id == existingSlots[0].Id).Name.ShouldBe("Brunch");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary><c>HandleAsync</c> blocks deletion of slot with entries.</summary>
    [Fact]
    public async Task HandleAsync_BlocksDeletionOfSlotWithEntries()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [("Breakfast", new TimeOnly(7, 0)), ("Lunch", new TimeOnly(12, 0))],
            TestClock.UtcNow);
        var existingSlots = existing.Slots.OrderBy(s => s.SortOrder).ToList();

        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(existing);
        _mealEntryRepository.AnyForSlotAsync(existingSlots[1].Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = NewCommand(new MealSlotInput(existingSlots[0].Id.Value, "Breakfast", "07:00"));

        var act = () => _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<DietPlannerDomainException>();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary><c>HandleAsync</c> allows deletion of unused slot.</summary>
    [Fact]
    public async Task HandleAsync_AllowsDeletionOfUnusedSlot()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [("Breakfast", new TimeOnly(7, 0)), ("Snack", new TimeOnly(15, 0))],
            TestClock.UtcNow);
        var existingSlots = existing.Slots.OrderBy(s => s.SortOrder).ToList();

        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(existing);
        _mealEntryRepository.AnyForSlotAsync(Arg.Any<MealSlotId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = NewCommand(new MealSlotInput(existingSlots[0].Id.Value, "Breakfast", "07:00"));

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        existing.Slots.Count.ShouldBe(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>Unit tests for <c>UpdateMealScheduleCommandValidator</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class UpdateMealScheduleCommandValidatorTests
{
    private readonly UpdateMealScheduleCommandValidator _sut = new();

    private static UpdateMealScheduleCommand ValidCommand(int slotCount = 2)
        => new(
            PersonId: Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            Slots: Enumerable.Range(1, slotCount)
                .Select(i => new MealSlotInput(null, $"Slot {i}", "08:00"))
                .ToList(),
            "user-1");

    /// <summary>With valid command: <c>Validate</c> returns no errors.</summary>
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var errors = _sut.Validate(ValidCommand()).ToList();

        errors.ShouldBeEmpty();
    }

    /// <summary>With empty person id: <c>Validate</c> returns error.</summary>
    [Fact]
    public void Validate_WithEmptyPersonId_ReturnsError()
    {
        var command = new UpdateMealScheduleCommand(Guid.Empty,
            [new MealSlotInput(null, "Breakfast", "07:00")], "user-1");

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.PersonId));
    }

    /// <summary>With zero slots: <c>Validate</c> returns error.</summary>
    [Fact]
    public void Validate_WithZeroSlots_ReturnsError()
    {
        var command = new UpdateMealScheduleCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), [], "user-1");

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Slots));
    }

    /// <summary>With nine slots: <c>Validate</c> returns error.</summary>
    [Fact]
    public void Validate_WithNineSlots_ReturnsError()
    {
        var command = new UpdateMealScheduleCommand(
            Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            Enumerable.Range(1, 9)
                .Select(i => new MealSlotInput(null, $"Slot {i}", "08:00"))
                .ToList(),
            "user-1");

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Slots));
    }

    /// <summary>With empty slot name: <c>Validate</c> returns error.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptySlotName_ReturnsError(string name)
    {
        var command = new UpdateMealScheduleCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [new MealSlotInput(null, name, "07:00")], "user-1");

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == "Slots[0].Name");
    }

    /// <summary>With invalid slot time: <c>Validate</c> returns error.</summary>
    [Theory]
    [InlineData("not-a-time")]
    [InlineData("25:00")]
    [InlineData("abc")]
    public void Validate_WithInvalidSlotTime_ReturnsError(string time)
    {
        var command = new UpdateMealScheduleCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"),
            [new MealSlotInput(null, "Breakfast", time)], "user-1");

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == "Slots[0].DefaultTime");
    }
}
