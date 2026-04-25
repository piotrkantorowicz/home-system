namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005
using DietPlanner.Application.Commands.UpdateMealSchedule;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

public sealed class UpdateMealScheduleCommandHandlerTests
{
    private readonly IMealScheduleConfigRepository _repository =
        Substitute.For<IMealScheduleConfigRepository>();
    private readonly IMealEntryRepository _mealEntryRepository =
        Substitute.For<IMealEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateMealScheduleCommandHandler _sut;

    public UpdateMealScheduleCommandHandlerTests()
        => _sut = new UpdateMealScheduleCommandHandler(_repository, _mealEntryRepository, _unitOfWork);

    private static UpdateMealScheduleCommand NewCommand(params MealSlotInput[] slots)
        => new("user-1", slots);

    [Fact]
    public async Task HandleAsync_WhenNoExistingConfig_CreatesNewAndCommits()
    {
        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((MealScheduleConfig?)null);

        var command = NewCommand(
            new MealSlotInput(null, "Breakfast", "07:00"),
            new MealSlotInput(null, "Lunch", "12:00"),
            new MealSlotInput(null, "Dinner", "18:00"));

        await _sut.HandleAsync(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<MealScheduleConfig>(c => c.UserId == "user-1" && c.Slots.Count == 3),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PreservesIdsForKeptSlots()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("Breakfast", new TimeOnly(7, 0)), ("Lunch", new TimeOnly(12, 0))]);
        var existingSlots = existing.Slots.OrderBy(s => s.SortOrder).ToList();

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(existing);
        _mealEntryRepository.AnyForSlotAsync(Arg.Any<MealSlotId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = NewCommand(
            new MealSlotInput(existingSlots[0].Id.Value, "Brunch", "10:00"),
            new MealSlotInput(existingSlots[1].Id.Value, "Lunch", "12:00"));

        await _sut.HandleAsync(command, CancellationToken.None);

        existing.Slots.Single(s => s.Id == existingSlots[0].Id).Name.ShouldBe("Brunch");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BlocksDeletionOfSlotWithEntries()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("Breakfast", new TimeOnly(7, 0)), ("Lunch", new TimeOnly(12, 0))]);
        var existingSlots = existing.Slots.OrderBy(s => s.SortOrder).ToList();

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(existing);
        _mealEntryRepository.AnyForSlotAsync(existingSlots[1].Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = NewCommand(new MealSlotInput(existingSlots[0].Id.Value, "Breakfast", "07:00"));

        var act = () => _sut.HandleAsync(command, CancellationToken.None);

        await act.ShouldThrowAsync<DietPlannerDomainException>();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AllowsDeletionOfUnusedSlot()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("Breakfast", new TimeOnly(7, 0)), ("Snack", new TimeOnly(15, 0))]);
        var existingSlots = existing.Slots.OrderBy(s => s.SortOrder).ToList();

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(existing);
        _mealEntryRepository.AnyForSlotAsync(Arg.Any<MealSlotId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = NewCommand(new MealSlotInput(existingSlots[0].Id.Value, "Breakfast", "07:00"));

        await _sut.HandleAsync(command, CancellationToken.None);

        existing.Slots.Count.ShouldBe(1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateMealScheduleCommandValidatorTests
{
    private readonly UpdateMealScheduleCommandValidator _sut = new();

    private static UpdateMealScheduleCommand ValidCommand(int slotCount = 2)
        => new(
            UserId: "user-1",
            Slots: Enumerable.Range(1, slotCount)
                .Select(i => new MealSlotInput(null, $"Slot {i}", "08:00"))
                .ToList());

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var errors = _sut.Validate(ValidCommand()).ToList();

        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyUserId_ReturnsError(string userId)
    {
        var command = new UpdateMealScheduleCommand(userId,
            [new MealSlotInput(null, "Breakfast", "07:00")]);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public void Validate_WithZeroSlots_ReturnsError()
    {
        var command = new UpdateMealScheduleCommand("user-1", []);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Slots));
    }

    [Fact]
    public void Validate_WithNineSlots_ReturnsError()
    {
        var command = new UpdateMealScheduleCommand(
            "user-1",
            Enumerable.Range(1, 9)
                .Select(i => new MealSlotInput(null, $"Slot {i}", "08:00"))
                .ToList());

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Slots));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptySlotName_ReturnsError(string name)
    {
        var command = new UpdateMealScheduleCommand("user-1",
            [new MealSlotInput(null, name, "07:00")]);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == "Slots[0].Name");
    }

    [Theory]
    [InlineData("not-a-time")]
    [InlineData("25:00")]
    [InlineData("abc")]
    public void Validate_WithInvalidSlotTime_ReturnsError(string time)
    {
        var command = new UpdateMealScheduleCommand("user-1",
            [new MealSlotInput(null, "Breakfast", time)]);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == "Slots[0].DefaultTime");
    }
}
