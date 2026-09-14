namespace Notifications.Application.Dispatching;

using Notifications.Domain.ValueObjects;

/// <summary>
/// The one entry point integration-event handlers use to create a notification: renders the
/// template for the user's locale, stores the notification, creates a delivery per enabled channel
/// and sends over every channel that has a sender. Runs inside the module's unit of work.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>Creates and sends a notification.</summary>
    /// <param name="type">Selects the template.</param>
    /// <param name="userId">Auth subject of the recipient.</param>
    /// <param name="locale">The recipient's locale; falls back to <c>en</c> when no template exists for it.</param>
    /// <param name="payload">JSON with the source event's identifiers, stored for deep links.</param>
    /// <param name="placeholders">Values for the template's <c>{{Name}}</c> placeholders, or <see langword="null"/> when it has none.</param>
    /// <param name="ct">Propagates cancellation to storage and senders.</param>
    Task DispatchAsync(
        NotificationType type,
        string userId,
        string locale,
        string payload,
        IReadOnlyDictionary<string, string>? placeholders,
        CancellationToken ct = default);
}
