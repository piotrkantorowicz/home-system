namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.UpdateMealSchedule;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

public sealed class UpdateMealScheduleCommandHandlerTests
{
    private readonly IMealScheduleConfigRepository _repository = Substitute.For<IMealScheduleConfigRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateMealScheduleCommandHandler _sut;

    private static readonly UpdateMealScheduleCommand ValidCommand = new(
        UserId: "user-1",
        Slots:
        [
            new MealSlotInput("Breakfast", "07:00"),
            new MealSlotInput("Lunch", "12:00"),
            new MealSlotInput("Dinner", "18:00")
        ]);

    public UpdateMealScheduleCommandHandlerTests()
        => _sut = new UpdateMealScheduleCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WhenNoExistingConfig_CreatesNewAndCommits()
    {
        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((MealScheduleConfig?)null);

        await _sut.HandleAsync(ValidCommand, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<MealScheduleConfig>(c => c.UserId == "user-1" && c.Slots.Count == 3),
            Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Update(Arg.Any<MealScheduleConfig>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenConfigExists_UpdatesAndCommits()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("OldSlot", new TimeOnly(9, 0))]);

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.HandleAsync(ValidCommand, CancellationToken.None);

        _repository.Received(1).Update(Arg.Is<MealScheduleConfig>(c => c.Slots.Count == 3));
        await _repository.DidNotReceive().AddAsync(Arg.Any<MealScheduleConfig>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenConfigExists_SetsUpdatedAt()
    {
        var existing = MealScheduleConfig.Create(
            MealScheduleConfigId.New(),
            "user-1",
            [("OldSlot", new TimeOnly(9, 0))]);

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.HandleAsync(ValidCommand, CancellationToken.None);

        existing.UpdatedAt.ShouldNotBeNull();
    }
}

public sealed class UpdateMealScheduleCommandValidatorTests
{
    private readonly UpdateMealScheduleCommandValidator _sut = new();

    private static UpdateMealScheduleCommand ValidCommand(int slotCount = 2)
        => new(
            UserId: "user-1",
            Slots: Enumerable.Range(1, slotCount)
                .Select(i => new MealSlotInput($"Slot {i}", "08:00"))
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
        var command = new UpdateMealScheduleCommand(userId, [new MealSlotInput("Breakfast", "07:00")]);

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
            Enumerable.Range(1, 9).Select(i => new MealSlotInput($"Slot {i}", "08:00")).ToList());

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Slots));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptySlotName_ReturnsError(string name)
    {
        var command = new UpdateMealScheduleCommand("user-1", [new MealSlotInput(name, "07:00")]);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == "Slots[0].Name");
    }

    [Theory]
    [InlineData("not-a-time")]
    [InlineData("25:00")]
    [InlineData("abc")]
    public void Validate_WithInvalidSlotTime_ReturnsError(string time)
    {
        var command = new UpdateMealScheduleCommand("user-1", [new MealSlotInput("Breakfast", time)]);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == "Slots[0].DefaultTime");
    }
}
