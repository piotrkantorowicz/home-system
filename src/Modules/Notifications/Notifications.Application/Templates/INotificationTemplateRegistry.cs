namespace Notifications.Application.Templates;

using Notifications.Domain.ValueObjects;

/// <summary>Looks up the template for a notification type and locale.</summary>
public interface INotificationTemplateRegistry
{
    /// <summary>Returns the template for the locale, or the <c>en</c> template when the locale has none.</summary>
    /// <param name="type">The notification type.</param>
    /// <param name="locale">Locale code, e.g. <c>pl</c>.</param>
    /// <exception cref="InvalidOperationException">No template exists for the type in any locale.</exception>
    NotificationTemplate Resolve(NotificationType type, string locale);
}
