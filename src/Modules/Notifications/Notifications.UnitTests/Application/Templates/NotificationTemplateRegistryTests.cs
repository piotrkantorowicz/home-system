namespace Notifications.UnitTests.Application.Templates;

using Notifications.Application.Templates;
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>NotificationTemplateRegistry</c>: locale resolution and fallback over the built-in templates.</summary>
public sealed class NotificationTemplateRegistryTests
{
    private readonly NotificationTemplateRegistry _sut = new();

    /// <summary>With registered locale: <c>Resolve</c> returns template for that locale.</summary>
    [Fact]
    public void Resolve_WithRegisteredLocale_ReturnsTemplateForThatLocale()
    {
        var template = _sut.Resolve(NotificationType.MealReminder, "en");

        template.Type.ShouldBe(NotificationType.MealReminder);
        template.Locale.ShouldBe("en");
        template.TitleFormat.ShouldNotBeNullOrEmpty();
    }

    /// <summary>With unknown locale: <c>Resolve</c> falls back to english.</summary>
    [Fact]
    public void Resolve_WithUnknownLocale_FallsBackToEnglish()
    {
        var template = _sut.Resolve(NotificationType.MealMissed, "de");

        template.Locale.ShouldBe("en");
    }

    /// <summary>With blank locale: <c>Resolve</c> throws.</summary>
    [Fact]
    public void Resolve_WithBlankLocale_Throws()
    {
        var act = () => _sut.Resolve(NotificationType.MealReminder, "  ");
        act.ShouldThrow<ArgumentException>();
    }
}
