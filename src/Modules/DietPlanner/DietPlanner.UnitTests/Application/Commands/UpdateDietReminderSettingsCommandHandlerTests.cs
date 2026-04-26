namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005
using DietPlanner.Application.Commands.UpdateDietReminderSettings;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class UpdateDietReminderSettingsCommandHandlerTests
{
    private readonly IDietReminderSettingsRepository _repository =
        Substitute.For<IDietReminderSettingsRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateDietReminderSettingsCommandHandler _sut;

    public UpdateDietReminderSettingsCommandHandlerTests()
        => _sut = new UpdateDietReminderSettingsCommandHandler(_repository, _unitOfWork);

    private static UpdateDietReminderSettingsCommand DefaultCommand(string userId = "user-1")
        => new(
            userId,
            MealRemindersEnabled: true,
            MealReminderLeadTimeMinutes: 15,
            MealMissedGraceMinutes: 30,
            WaterRemindersEnabled: true,
            WaterReminderIntervalMinutes: 60,
            WaterWindowStartUtc: new TimeOnly(6, 0),
            WaterWindowEndUtc: new TimeOnly(22, 0),
            WeeklySummaryEnabled: true,
            WeeklySummaryDayOfWeekUtc: DayOfWeek.Sunday,
            WeeklySummaryTimeOfDayUtc: new TimeOnly(8, 0),
            GoalAlertsEnabled: true);

    [Fact]
    public async Task HandleAsync_WhenNoExistingSettings_CreatesNewAndCommits()
    {
        var command = DefaultCommand();

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((DietReminderSettings?)null);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<DietReminderSettings>(s =>
                s.UserId == "user-1" &&
                s.MealRemindersEnabled &&
                s.MealReminderLeadTimeMinutes == 15 &&
                s.MealMissedGraceMinutes == 30 &&
                s.WaterRemindersEnabled &&
                s.WeeklySummaryDayOfWeekUtc == DayOfWeek.Sunday &&
                s.GoalAlertsEnabled),
            Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Update(Arg.Any<DietReminderSettings>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenExistingSettings_UpdatesAndCommits()
    {
        var existing = DietReminderSettings.Create(DietReminderSettingsId.New(), "user-1");
        var command = DefaultCommand() with
        {
            MealRemindersEnabled = false,
            MealReminderLeadTimeMinutes = 45,
            WaterReminderIntervalMinutes = 90,
            GoalAlertsEnabled = false,
        };

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(existing);

        await _sut.HandleAsync(command, CancellationToken.None);

        _repository.Received(1).Update(
            Arg.Is<DietReminderSettings>(s =>
                s.MealRemindersEnabled == false &&
                s.MealReminderLeadTimeMinutes == 45 &&
                s.WaterReminderIntervalMinutes == 90 &&
                s.GoalAlertsEnabled == false));
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<DietReminderSettings>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateDietReminderSettingsCommandValidatorTests
{
    private readonly UpdateDietReminderSettingsCommandValidator _sut = new();

    private static UpdateDietReminderSettingsCommand DefaultCommand(string userId = "user-1")
        => new(
            userId,
            MealRemindersEnabled: true,
            MealReminderLeadTimeMinutes: 15,
            MealMissedGraceMinutes: 30,
            WaterRemindersEnabled: true,
            WaterReminderIntervalMinutes: 60,
            WaterWindowStartUtc: new TimeOnly(6, 0),
            WaterWindowEndUtc: new TimeOnly(22, 0),
            WeeklySummaryEnabled: true,
            WeeklySummaryDayOfWeekUtc: DayOfWeek.Sunday,
            WeeklySummaryTimeOfDayUtc: new TimeOnly(8, 0),
            GoalAlertsEnabled: true);

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var errors = _sut.Validate(DefaultCommand()).ToList();
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsValidationError()
    {
        var command = DefaultCommand("");
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Validate_WithInvalidMealLeadTimeMinutes_ReturnsValidationError(int leadTime)
    {
        var command = DefaultCommand() with { MealReminderLeadTimeMinutes = leadTime };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.MealReminderLeadTimeMinutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(241)]
    public void Validate_WithInvalidGraceMinutes_ReturnsValidationError(int grace)
    {
        var command = DefaultCommand() with { MealMissedGraceMinutes = grace };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.MealMissedGraceMinutes));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(481)]
    public void Validate_WithInvalidWaterReminderIntervalMinutes_ReturnsValidationError(int interval)
    {
        var command = DefaultCommand() with { WaterReminderIntervalMinutes = interval };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.WaterReminderIntervalMinutes));
    }

    [Fact]
    public void Validate_WhenWaterWindowEndNotAfterStart_ReturnsValidationError()
    {
        var command = DefaultCommand() with
        {
            WaterWindowStartUtc = new TimeOnly(12, 0),
            WaterWindowEndUtc = new TimeOnly(11, 0),
        };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.WaterWindowEndUtc));
    }
}
