namespace Notifications.Application.Channels;

using Notifications.Domain.ValueObjects;

/// <summary>
/// Delivers notifications over one channel. Implementations are registered per channel; a channel
/// enabled in preferences but without a sender gets its deliveries skipped. Throwing is treated the
/// same as returning <see cref="DeliveryOutcome.Failed"/>.
/// </summary>
public interface INotificationChannelSender
{
    /// <summary>The channel this sender handles.</summary>
    NotificationChannel Channel { get; }

    /// <summary>Attempts one delivery.</summary>
    /// <param name="context">The notification content and delivery identifiers.</param>
    /// <param name="ct">Propagates cancellation to the send.</param>
    /// <returns>What happened, for the dispatcher to record.</returns>
    Task<DeliveryOutcome> SendAsync(NotificationSendContext context, CancellationToken ct = default);
}
