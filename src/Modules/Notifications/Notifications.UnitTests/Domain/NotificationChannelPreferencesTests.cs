namespace Notifications.UnitTests.Domain;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>NotificationChannelPreferences</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class NotificationChannelPreferencesTests
{
    /// <summary><c>CreateDefault</c> all channels enabled.</summary>
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

    /// <summary><c>Update</c> applies new values and updated at.</summary>
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

    /// <summary><c>IsEnabled</c> returns corresponding flag.</summary>
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
