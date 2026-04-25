namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.Commands.UpdateNotificationPreferences;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class UpdateNotificationPreferencesCommandHandlerTests
{
    private readonly INotificationPreferencesRepository _repository =
        Substitute.For<INotificationPreferencesRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateNotificationPreferencesCommandHandler _sut;

    public UpdateNotificationPreferencesCommandHandlerTests()
        => _sut = new UpdateNotificationPreferencesCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_WhenNoExistingPreferences_CreatesNewAndCommits()
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", true, 15, true, 60, true, true);

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((NotificationPreferences?)null);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<NotificationPreferences>(p =>
                p.UserId == "user-1" &&
                p.MealReminderEnabled &&
                p.MealReminderLeadTimeMinutes == 15 &&
                p.WaterReminderEnabled &&
                p.WaterReminderIntervalMinutes == 60 &&
                p.WeeklySummaryEnabled &&
                p.GoalMilestoneAlertsEnabled),
            Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Update(Arg.Any<NotificationPreferences>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNoExistingPreferences_CreatesWithDisabledSettings()
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-2", false, 30, false, 120, false, false);

        _repository.GetByUserIdAsync("user-2", Arg.Any<CancellationToken>())
            .Returns((NotificationPreferences?)null);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<NotificationPreferences>(p =>
                p.MealReminderEnabled == false &&
                p.MealReminderLeadTimeMinutes == 30 &&
                p.WaterReminderEnabled == false &&
                p.WaterReminderIntervalMinutes == 120 &&
                p.WeeklySummaryEnabled == false &&
                p.GoalMilestoneAlertsEnabled == false),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenExistingPreferences_UpdatesAndCommits()
    {
        var existing = NotificationPreferences.Create(
            NotificationPreferencesId.New(), "user-1");
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", false, 45, false, 90, false, false);

        _repository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(existing);

        await _sut.HandleAsync(command, CancellationToken.None);

        _repository.Received(1).Update(
            Arg.Is<NotificationPreferences>(p =>
                p.MealReminderEnabled == false &&
                p.MealReminderLeadTimeMinutes == 45 &&
                p.WaterReminderEnabled == false &&
                p.WaterReminderIntervalMinutes == 90 &&
                p.WeeklySummaryEnabled == false &&
                p.GoalMilestoneAlertsEnabled == false));
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<NotificationPreferences>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateNotificationPreferencesCommandValidatorTests
{
    private readonly UpdateNotificationPreferencesCommandValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", true, 15, true, 60, true, true);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsValidationError()
    {
        var command = new UpdateNotificationPreferencesCommand(
            "", true, 15, true, 60, true, true);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Validate_WithInvalidMealLeadTimeMinutes_ReturnsValidationError(int leadTime)
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", true, leadTime, true, 60, true, true);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.MealReminderLeadTimeMinutes));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(120)]
    public void Validate_WithValidMealLeadTimeMinutes_ReturnsNoError(int leadTime)
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", true, leadTime, true, 60, true, true);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldNotContain(e => e.PropertyName == nameof(command.MealReminderLeadTimeMinutes));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(0)]
    [InlineData(481)]
    public void Validate_WithInvalidWaterReminderIntervalMinutes_ReturnsValidationError(int interval)
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", true, 15, true, interval, true, true);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.WaterReminderIntervalMinutes));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(60)]
    [InlineData(480)]
    public void Validate_WithValidWaterReminderIntervalMinutes_ReturnsNoError(int interval)
    {
        var command = new UpdateNotificationPreferencesCommand(
            "user-1", true, 15, true, interval, true, true);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldNotContain(e => e.PropertyName == nameof(command.WaterReminderIntervalMinutes));
    }
}
