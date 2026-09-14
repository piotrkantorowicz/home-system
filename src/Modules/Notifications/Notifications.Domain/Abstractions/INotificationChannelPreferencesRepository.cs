namespace Notifications.Domain.Abstractions;

using Notifications.Domain.Models;

/// <summary>
/// Dapper-backed access to <see cref="NotificationChannelPreferences"/>. Writes execute immediately
/// on the unit of work's transaction and become durable on <c>CommitAsync</c>.
/// </summary>
public interface INotificationChannelPreferencesRepository
{
    /// <summary>Loads the user's preferences.</summary>
    /// <param name="userId">Auth subject of the user.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The row, or <see langword="null"/> when none has been created yet.</returns>
    Task<NotificationChannelPreferences?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    /// <summary>Inserts a new preferences row.</summary>
    /// <param name="preferences">The row to insert.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(NotificationChannelPreferences preferences, CancellationToken ct = default);
    /// <summary>Writes the switches of an existing row.</summary>
    /// <param name="preferences">The row to update.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task UpdateAsync(NotificationChannelPreferences preferences, CancellationToken ct = default);
}
