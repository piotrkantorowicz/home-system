namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>DietReminderSettings</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class DietReminderSettingsTests
{
    /// <summary>With defaults: <c>Create</c> applies expected values.</summary>
    [Fact]
    public void Create_WithDefaults_AppliesExpectedValues()
    {
        var id = DietReminderSettingsId.New();

        var settings = DietReminderSettings.Create(id, "user-1", TestClock.UtcNow);

        settings.Id.ShouldBe(id);
        settings.UserId.ShouldBe("user-1");
        settings.MealRemindersEnabled.ShouldBeTrue();
        settings.MealReminderLeadTimeMinutes.ShouldBe(15);
        settings.MealMissedGraceMinutes.ShouldBe(30);
        settings.WaterRemindersEnabled.ShouldBeTrue();
        settings.WaterReminderIntervalMinutes.ShouldBe(60);
        settings.WaterWindowStartUtc.ShouldBe(new TimeOnly(6, 0));
        settings.WaterWindowEndUtc.ShouldBe(new TimeOnly(22, 0));
        settings.WeeklySummaryEnabled.ShouldBeTrue();
        settings.WeeklySummaryDayOfWeekUtc.ShouldBe(DayOfWeek.Sunday);
        settings.WeeklySummaryTimeOfDayUtc.ShouldBe(new TimeOnly(8, 0));
        settings.GoalAlertsEnabled.ShouldBeTrue();
        settings.UpdatedAt.ShouldBeNull();
    }

    /// <summary>With custom values: <c>Create</c> applies all settings.</summary>
    [Fact]
    public void Create_WithCustomValues_AppliesAllSettings()
    {
        var id = DietReminderSettingsId.New();

        var settings = DietReminderSettings.Create(
            id, "user-2",
            TestClock.UtcNow,
            mealRemindersEnabled: false,
            mealReminderLeadTimeMinutes: 30,
            mealMissedGraceMinutes: 15,
            waterRemindersEnabled: false,
            waterReminderIntervalMinutes: 120,
            waterWindowStartUtc: new TimeOnly(7, 30),
            waterWindowEndUtc: new TimeOnly(20, 0),
            weeklySummaryEnabled: false,
            weeklySummaryDayOfWeekUtc: DayOfWeek.Monday,
            weeklySummaryTimeOfDayUtc: new TimeOnly(9, 0),
            goalAlertsEnabled: false);

        settings.MealRemindersEnabled.ShouldBeFalse();
        settings.MealReminderLeadTimeMinutes.ShouldBe(30);
        settings.MealMissedGraceMinutes.ShouldBe(15);
        settings.WaterRemindersEnabled.ShouldBeFalse();
        settings.WaterReminderIntervalMinutes.ShouldBe(120);
        settings.WaterWindowStartUtc.ShouldBe(new TimeOnly(7, 30));
        settings.WaterWindowEndUtc.ShouldBe(new TimeOnly(20, 0));
        settings.WeeklySummaryDayOfWeekUtc.ShouldBe(DayOfWeek.Monday);
        settings.WeeklySummaryTimeOfDayUtc.ShouldBe(new TimeOnly(9, 0));
        settings.GoalAlertsEnabled.ShouldBeFalse();
    }

    /// <summary>With null user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => DietReminderSettings.Create(DietReminderSettingsId.New(), null!, TestClock.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With whitespace user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithWhitespaceUserId_ThrowsArgumentException()
    {
        var act = () => DietReminderSettings.Create(DietReminderSettingsId.New(), "   ", TestClock.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With non positive lead time: <c>Create</c> throws domain exception.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveLeadTime_ThrowsDomainException(int leadTime)
    {
        var act = () => DietReminderSettings.Create(
            DietReminderSettingsId.New(), "user-1",
            TestClock.UtcNow,
            mealReminderLeadTimeMinutes: leadTime);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>With non positive grace minutes: <c>Create</c> throws domain exception.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveGraceMinutes_ThrowsDomainException(int grace)
    {
        var act = () => DietReminderSettings.Create(
            DietReminderSettingsId.New(), "user-1",
            TestClock.UtcNow,
            mealMissedGraceMinutes: grace);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>When water window end not after start: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WhenWaterWindowEndNotAfterStart_ThrowsDomainException()
    {
        var act = () => DietReminderSettings.Create(
            DietReminderSettingsId.New(), "user-1",
            TestClock.UtcNow,
            waterWindowStartUtc: new TimeOnly(10, 0),
            waterWindowEndUtc: new TimeOnly(10, 0));

        act.ShouldThrow<DietPlannerDomainException>()
           .Message.ShouldContain("WaterWindowEndUtc");
    }

    /// <summary>With new values: <c>Update</c> updates all properties.</summary>
    [Fact]
    public void Update_WithNewValues_UpdatesAllProperties()
    {
        var settings = DietReminderSettings.Create(DietReminderSettingsId.New(), "user-1", TestClock.UtcNow);

        settings.Update(
            mealRemindersEnabled: false,
            mealReminderLeadTimeMinutes: 45,
            mealMissedGraceMinutes: 20,
            waterRemindersEnabled: false,
            waterReminderIntervalMinutes: 90,
            waterWindowStartUtc: new TimeOnly(8, 0),
            waterWindowEndUtc: new TimeOnly(18, 0),
            weeklySummaryEnabled: false,
            weeklySummaryDayOfWeekUtc: DayOfWeek.Friday,
            weeklySummaryTimeOfDayUtc: new TimeOnly(17, 30),
            goalAlertsEnabled: false,
            TestClock.UtcNow);

        settings.MealRemindersEnabled.ShouldBeFalse();
        settings.MealReminderLeadTimeMinutes.ShouldBe(45);
        settings.MealMissedGraceMinutes.ShouldBe(20);
        settings.WaterReminderIntervalMinutes.ShouldBe(90);
        settings.WaterWindowStartUtc.ShouldBe(new TimeOnly(8, 0));
        settings.WaterWindowEndUtc.ShouldBe(new TimeOnly(18, 0));
        settings.WeeklySummaryDayOfWeekUtc.ShouldBe(DayOfWeek.Friday);
        settings.WeeklySummaryTimeOfDayUtc.ShouldBe(new TimeOnly(17, 30));
        settings.GoalAlertsEnabled.ShouldBeFalse();
        settings.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>When water window end not after start: <c>Update</c> throws domain exception.</summary>
    [Fact]
    public void Update_WhenWaterWindowEndNotAfterStart_ThrowsDomainException()
    {
        var settings = DietReminderSettings.Create(DietReminderSettingsId.New(), "user-1", TestClock.UtcNow);

        var act = () => settings.Update(
            mealRemindersEnabled: true,
            mealReminderLeadTimeMinutes: 15,
            mealMissedGraceMinutes: 30,
            waterRemindersEnabled: true,
            waterReminderIntervalMinutes: 60,
            waterWindowStartUtc: new TimeOnly(12, 0),
            waterWindowEndUtc: new TimeOnly(11, 0),
            weeklySummaryEnabled: true,
            weeklySummaryDayOfWeekUtc: DayOfWeek.Sunday,
            weeklySummaryTimeOfDayUtc: new TimeOnly(8, 0),
            goalAlertsEnabled: true,
            TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}
