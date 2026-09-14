namespace Notifications.Application.Commands.AckNotificationDelivery;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks a WebSocket delivery as sent once the browser confirms it displayed the notification.
/// </summary>
/// <param name="DeliveryId">The delivery being acknowledged; must belong to the caller.</param>
/// <param name="UserId">Auth subject of the caller; only their own rows are touched.</param>
public sealed record AckNotificationDeliveryCommand(Guid DeliveryId, string UserId) : ICommand;
