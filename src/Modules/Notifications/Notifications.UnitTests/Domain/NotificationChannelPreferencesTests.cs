namespace Notifications.UnitTests.Domain;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

public sealed class NotificationChannelPreferencesTests
{
    [Fact]
    public void CreateDefault_AllChannelsEnabled()
    {
        var prefs = NotificationChannelPreferences.CreateDefault(
            NotificationChannelPreferencesId.New(),
            "user-1",
            DateTime.UtcNow);

        prefs.ConsoleEnabled.ShouldBeTrue();
        prefs.EmailEnabled.ShouldBeTrue();
        prefs.WebSocketEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Update_AppliesNewValuesAndUpdatedAt()
    {
        var prefs = NotificationChannelPreferences.CreateDefault(
            NotificationChannelPreferencesId.New(), "user-1", DateTime.UtcNow);
        var newAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

        prefs.Update(consoleEnabled: false, emailEnabled: false, webSocketEnabled: true, updatedAt: newAt);

        prefs.ConsoleEnabled.ShouldBeFalse();
        prefs.EmailEnabled.ShouldBeFalse();
        prefs.WebSocketEnabled.ShouldBeTrue();
        prefs.UpdatedAt.ShouldBe(newAt);
    }

    [Theory]
    [InlineData(NotificationChannel.Console, true, false, false, true)]
    [InlineData(NotificationChannel.Email, false, true, false, true)]
    [InlineData(NotificationChannel.WebSocket, false, false, true, true)]
    [InlineData(NotificationChannel.Console, false, true, true, false)]
    public void IsEnabled_ReturnsCorrespondingFlag(
        NotificationChannel channel, bool console, bool email, bool ws, bool expected)
    {
        var prefs = NotificationChannelPreferences.CreateDefault(
            NotificationChannelPreferencesId.New(), "user-1", DateTime.UtcNow);
        prefs.Update(console, email, ws, DateTime.UtcNow);

        prefs.IsEnabled(channel).ShouldBe(expected);
    }
}
