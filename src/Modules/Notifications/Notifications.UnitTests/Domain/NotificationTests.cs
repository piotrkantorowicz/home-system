namespace Notifications.UnitTests.Domain;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

public sealed class NotificationTests
{
    [Fact]
    public void Create_WithValidData_PopulatesProperties()
    {
        var id = NotificationId.New();
        var createdAt = new DateTime(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc);

        var notification = Notification.Create(
            id, "user-1", NotificationType.MealReminder,
            "Lunch reminder", "It's lunch time",
            """{"mealEntryId":"abc"}""", createdAt);

        notification.Id.ShouldBe(id);
        notification.UserId.ShouldBe("user-1");
        notification.Type.ShouldBe(NotificationType.MealReminder);
        notification.Title.ShouldBe("Lunch reminder");
        notification.Body.ShouldBe("It's lunch time");
        notification.CreatedAt.ShouldBe(createdAt);
        notification.ReadAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithBlankUserId_Throws()
    {
        var act = () => Notification.Create(
            NotificationId.New(), "  ", NotificationType.WaterReminder,
            "T", "B", "{}", DateTime.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void MarkRead_FirstTime_SetsReadAt()
    {
        var notification = NewMealReminder();
        var readAt = DateTime.UtcNow;

        notification.MarkRead(readAt);

        notification.ReadAt.ShouldBe(readAt);
    }

    [Fact]
    public void MarkRead_WhenAlreadyRead_DoesNotOverwrite()
    {
        var notification = NewMealReminder();
        var firstRead = new DateTime(2026, 4, 26, 9, 0, 0, DateTimeKind.Utc);
        notification.MarkRead(firstRead);

        notification.MarkRead(new DateTime(2026, 4, 26, 11, 0, 0, DateTimeKind.Utc));

        notification.ReadAt.ShouldBe(firstRead);
    }

    private static Notification NewMealReminder()
        => Notification.Create(
            NotificationId.New(), "user-1", NotificationType.MealReminder,
            "T", "B", "{}", DateTime.UtcNow);
}
