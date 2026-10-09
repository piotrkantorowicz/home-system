namespace Notifications.IntegrationTests.Queries;

using Notifications.Application.Templates;
using Notifications.Infrastructure.Queries;

/// <summary>Inbox message localization from persisted notification payloads.</summary>
public sealed class NotificationTextLocalizerTests
{
    private readonly NotificationTextLocalizer _sut = new(new NotificationTemplateRegistry());
    private static readonly DateTime CreatedAt = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Meal reminders use stored slot name and planned time in Polish template.</summary>
    [Fact]
    public void Localize_PolishMealReminder_RendersStoredValues()
    {
        var row = new NotificationListRow(Guid.CreateVersion7(), "MealReminder", "Time for lunch",
            "Your lunch is planned at 12:00.", """{"MealSlotName":"lunch","PlannedAt":"2026-05-01T12:00:00+02:00"}""",
            CreatedAt, null);

        var result = _sut.Localize(row, "pl");

        result.Title.ShouldBe("Czas na lunch");
        result.Body.ShouldBe("Twój posiłek lunch zaplanowano na 12:00.");
    }

    /// <summary>Goal milestone body is rendered from numeric target.</summary>
    [Fact]
    public void Localize_PolishGoalMilestone_RendersValue()
    {
        var row = new NotificationListRow(Guid.CreateVersion7(), "GoalMilestone", "Goal reached",
            "Reached target weight of 75 kg", """{"Value":75}""", CreatedAt, null);

        var result = _sut.Localize(row, "pl");

        result.Title.ShouldBe("Cel osiągnięty");
        result.Body.ShouldBe("Osiągnięto docelową masę ciała 75 kg.");
    }

    /// <summary>Older notifications without complete payload retain stored text.</summary>
    [Fact]
    public void Localize_MissingPayload_KeepsStoredText()
    {
        var row = new NotificationListRow(Guid.CreateVersion7(), "MealReminder", "Time for lunch",
            "Your lunch is planned at 12:00.", "{}", CreatedAt, null);

        var result = _sut.Localize(row, "pl");

        result.Title.ShouldBe(row.Title);
        result.Body.ShouldBe(row.Body);
    }
}
