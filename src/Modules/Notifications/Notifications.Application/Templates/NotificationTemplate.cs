namespace Notifications.Application.Templates;

using Notifications.Domain.ValueObjects;

/// <summary>
/// Title and body formats for one notification type in one locale. Placeholders use <c>{{Name}}</c> and are filled from the dispatch call's placeholder dictionary.
/// </summary>
/// <param name="Type">Notification type the template renders.</param>
/// <param name="Locale">Locale code, e.g. <c>en</c> or <c>pl</c>.</param>
/// <param name="TitleFormat">Title with <c>{{Name}}</c> placeholders.</param>
/// <param name="BodyFormat">Body with <c>{{Name}}</c> placeholders.</param>
public sealed record NotificationTemplate(
    NotificationType Type,
    string Locale,
    string TitleFormat,
    string BodyFormat);
