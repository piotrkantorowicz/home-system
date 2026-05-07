namespace Notifications.Application.Commands.AckNotificationDelivery;

using Shared.Abstractions.Cqrs;

public sealed record AckNotificationDeliveryCommand(Guid DeliveryId, string UserId) : ICommand;
