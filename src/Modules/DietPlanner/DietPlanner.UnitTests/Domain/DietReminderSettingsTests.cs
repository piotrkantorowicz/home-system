namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class DietReminderSettingsTests
{
    [Fact]
    public void Create_WithDefaults_AppliesExpectedValues()
    {
        var id = DietReminderSettingsId.New();

        var settings = DietReminderSettings.Create(id, "user-1");

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

    [Fact]
    public void Create_WithCustomValues_AppliesAllSettings()
    {
        var id = DietReminderSettingsId.New();

        var settings = DietReminderSettings.Create(
            id, "user-2",
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

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => DietReminderSettings.Create(DietReminderSettingsId.New(), null!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceUserId_ThrowsArgumentException()
    {
        var act = () => DietReminderSettings.Create(DietReminderSettingsId.New(), "   ");

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveLeadTime_ThrowsDomainException(int leadTime)
    {
        var act = () => DietReminderSettings.Create(
            DietReminderSettingsId.New(), "user-1",
            mealReminderLeadTimeMinutes: leadTime);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveGraceMinutes_ThrowsDomainException(int grace)
    {
        var act = () => DietReminderSettings.Create(
            DietReminderSettingsId.New(), "user-1",
            mealMissedGraceMinutes: grace);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void Create_WhenWaterWindowEndNotAfterStart_ThrowsDomainException()
    {
        var act = () => DietReminderSettings.Create(
            DietReminderSettingsId.New(), "user-1",
            waterWindowStartUtc: new TimeOnly(10, 0),
            waterWindowEndUtc: new TimeOnly(10, 0));

        act.ShouldThrow<DietPlannerDomainException>()
           .Message.ShouldContain("WaterWindowEndUtc");
    }

    [Fact]
    public void Update_WithNewValues_UpdatesAllProperties()
    {
        var settings = DietReminderSettings.Create(DietReminderSettingsId.New(), "user-1");

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
            goalAlertsEnabled: false);

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

    [Fact]
    public void Update_WhenWaterWindowEndNotAfterStart_ThrowsDomainException()
    {
        var settings = DietReminderSettings.Create(DietReminderSettingsId.New(), "user-1");

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
            goalAlertsEnabled: true);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}
