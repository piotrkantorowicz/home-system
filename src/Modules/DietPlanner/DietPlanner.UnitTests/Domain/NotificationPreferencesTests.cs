namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public sealed class NotificationPreferencesTests
{
    [Fact]
    public void Create_WithValidData_CreatesNotificationPreferences()
    {
        var id = NotificationPreferencesId.New();

        var prefs = NotificationPreferences.Create(id, "user-1");

        prefs.Id.ShouldBe(id);
        prefs.UserId.ShouldBe("user-1");
        prefs.MealReminderEnabled.ShouldBeTrue();
        prefs.MealReminderLeadTimeMinutes.ShouldBe(15);
        prefs.WaterReminderEnabled.ShouldBeTrue();
        prefs.WaterReminderIntervalMinutes.ShouldBe(60);
        prefs.WeeklySummaryEnabled.ShouldBeTrue();
        prefs.GoalMilestoneAlertsEnabled.ShouldBeTrue();
        prefs.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithCustomValues_AppliesAllSettings()
    {
        var id = NotificationPreferencesId.New();

        var prefs = NotificationPreferences.Create(
            id, "user-2",
            mealReminderEnabled: false,
            mealReminderLeadTimeMinutes: 30,
            waterReminderEnabled: false,
            waterReminderIntervalMinutes: 120,
            weeklySummaryEnabled: false,
            goalMilestoneAlertsEnabled: false);

        prefs.MealReminderEnabled.ShouldBeFalse();
        prefs.MealReminderLeadTimeMinutes.ShouldBe(30);
        prefs.WaterReminderEnabled.ShouldBeFalse();
        prefs.WaterReminderIntervalMinutes.ShouldBe(120);
        prefs.WeeklySummaryEnabled.ShouldBeFalse();
        prefs.GoalMilestoneAlertsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => NotificationPreferences.Create(NotificationPreferencesId.New(), null!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceUserId_ThrowsArgumentException()
    {
        var act = () => NotificationPreferences.Create(NotificationPreferencesId.New(), "   ");

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Update_WithNewValues_UpdatesAllProperties()
    {
        var prefs = NotificationPreferences.Create(NotificationPreferencesId.New(), "user-1");

        prefs.Update(
            mealReminderEnabled: false,
            mealReminderLeadTimeMinutes: 45,
            waterReminderEnabled: false,
            waterReminderIntervalMinutes: 90,
            weeklySummaryEnabled: false,
            goalMilestoneAlertsEnabled: false);

        prefs.MealReminderEnabled.ShouldBeFalse();
        prefs.MealReminderLeadTimeMinutes.ShouldBe(45);
        prefs.WaterReminderEnabled.ShouldBeFalse();
        prefs.WaterReminderIntervalMinutes.ShouldBe(90);
        prefs.WeeklySummaryEnabled.ShouldBeFalse();
        prefs.GoalMilestoneAlertsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        var prefs = NotificationPreferences.Create(NotificationPreferencesId.New(), "user-1");
        prefs.UpdatedAt.ShouldBeNull();

        prefs.Update(false, 30, false, 120, false, false);

        prefs.UpdatedAt.ShouldNotBeNull();
    }
}
