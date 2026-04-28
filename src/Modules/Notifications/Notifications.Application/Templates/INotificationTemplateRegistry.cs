namespace Notifications.Application.Templates;

using Notifications.Domain.ValueObjects;

public interface INotificationTemplateRegistry
{
    NotificationTemplate Resolve(NotificationType type, string locale);
}
