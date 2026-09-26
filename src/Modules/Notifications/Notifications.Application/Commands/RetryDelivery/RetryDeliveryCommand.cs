namespace Notifications.Application.Commands.RetryDelivery;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin action: puts a failed (typically dead-lettered) delivery back in the retry worker's queue.
/// </summary>
/// <param name="DeliveryId">The delivery to retry.</param>
public sealed record RetryDeliveryCommand(Guid DeliveryId) : ICommand;
