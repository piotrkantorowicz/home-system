namespace Notifications.Application.Commands.RetryAllDeliveries;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin action: retries every dead-lettered delivery at once (up to
/// <see cref="MaxBatch"/> per call), each exactly like <c>RetryDeliveryCommand</c>.
/// </summary>
public sealed record RetryAllDeliveriesCommand : ICommand<RetryAllDeliveriesResultDto>
{
    /// <summary>Most deliveries retried by one call; a larger backlog needs another call.</summary>
    public const int MaxBatch = 500;
}
