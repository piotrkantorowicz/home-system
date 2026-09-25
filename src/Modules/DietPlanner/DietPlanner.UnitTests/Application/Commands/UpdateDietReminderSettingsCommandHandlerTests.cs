namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.UpdateDietReminderSettings;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>UpdateDietReminderSettingsCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class UpdateDietReminderSettingsCommandHandlerTests
{
    private readonly IDietReminderSettingsRepository _repository =
        Substitute.For<IDietReminderSettingsRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly UpdateDietReminderSettingsCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public UpdateDietReminderSettingsCommandHandlerTests()
        => _sut = new UpdateDietReminderSettingsCommandHandler(_repository, _unitOfWork, _clock);

    private static UpdateDietReminderSettingsCommand DefaultCommand(Guid personId)
        => new(
            personId,
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

    /// <summary>When no existing settings: <c>HandleAsync</c> creates new and commits.</summary>
    [Fact]
    public async Task HandleAsync_WhenNoExistingSettings_CreatesNewAndCommits()
    {
        var command = DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));

        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>())
            .Returns((DietReminderSettings?)null);

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        await _repository.Received(1).AddAsync(
            Arg.Is<DietReminderSettings>(s =>
                s.PersonId == Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb") &&
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

    /// <summary>When existing settings: <c>HandleAsync</c> updates and commits.</summary>
    [Fact]
    public async Task HandleAsync_WhenExistingSettings_UpdatesAndCommits()
    {
        var existing = DietReminderSettings.Create(DietReminderSettingsId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.UtcNow);
        var command = DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb")) with
        {
            MealRemindersEnabled = false,
            MealReminderLeadTimeMinutes = 45,
            WaterReminderIntervalMinutes = 90,
            GoalAlertsEnabled = false,
        };

        _repository.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>())
            .Returns(existing);

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

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

/// <summary>Unit tests for <c>UpdateDietReminderSettingsCommandValidator</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class UpdateDietReminderSettingsCommandValidatorTests
{
    private readonly UpdateDietReminderSettingsCommandValidator _sut = new();

    private static UpdateDietReminderSettingsCommand DefaultCommand(Guid personId)
        => new(
            personId,
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

    /// <summary>With valid command: <c>Validate</c> returns no errors.</summary>
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var errors = _sut.Validate(DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"))).ToList();
        errors.ShouldBeEmpty();
    }

    /// <summary>With empty user id: <c>Validate</c> returns validation error.</summary>
    [Fact]
    public void Validate_WithEmptyPersonId_ReturnsValidationError()
    {
        var command = DefaultCommand(Guid.Empty);
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.PersonId));
    }

    /// <summary>With invalid meal lead time minutes: <c>Validate</c> returns validation error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Validate_WithInvalidMealLeadTimeMinutes_ReturnsValidationError(int leadTime)
    {
        var command = DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb")) with { MealReminderLeadTimeMinutes = leadTime };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.MealReminderLeadTimeMinutes));
    }

    /// <summary>With invalid grace minutes: <c>Validate</c> returns validation error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(241)]
    public void Validate_WithInvalidGraceMinutes_ReturnsValidationError(int grace)
    {
        var command = DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb")) with { MealMissedGraceMinutes = grace };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.MealMissedGraceMinutes));
    }

    /// <summary>With invalid water reminder interval minutes: <c>Validate</c> returns validation error.</summary>
    [Theory]
    [InlineData(14)]
    [InlineData(481)]
    public void Validate_WithInvalidWaterReminderIntervalMinutes_ReturnsValidationError(int interval)
    {
        var command = DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb")) with { WaterReminderIntervalMinutes = interval };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.WaterReminderIntervalMinutes));
    }

    /// <summary>When water window end not after start: <c>Validate</c> returns validation error.</summary>
    [Fact]
    public void Validate_WhenWaterWindowEndNotAfterStart_ReturnsValidationError()
    {
        var command = DefaultCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb")) with
        {
            WaterWindowStartUtc = new TimeOnly(12, 0),
            WaterWindowEndUtc = new TimeOnly(11, 0),
        };
        var errors = _sut.Validate(command).ToList();
        errors.ShouldContain(e => e.PropertyName == nameof(command.WaterWindowEndUtc));
    }
}
