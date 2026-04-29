namespace Notifications.UnitTests.Application.Templates;

using Notifications.Application.Templates;
using Notifications.Domain.ValueObjects;

public sealed class NotificationTemplateRegistryTests
{
    private readonly NotificationTemplateRegistry _sut = new();

    [Fact]
    public void Resolve_WithRegisteredLocale_ReturnsTemplateForThatLocale()
    {
        var template = _sut.Resolve(NotificationType.MealReminder, "en");

        template.Type.ShouldBe(NotificationType.MealReminder);
        template.Locale.ShouldBe("en");
        template.TitleFormat.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Resolve_WithUnknownLocale_FallsBackToEnglish()
    {
        var template = _sut.Resolve(NotificationType.MealMissed, "de");

        template.Locale.ShouldBe("en");
    }

    [Fact]
    public void Resolve_WithBlankLocale_Throws()
    {
        var act = () => _sut.Resolve(NotificationType.MealReminder, "  ");
        act.ShouldThrow<ArgumentException>();
    }
}
