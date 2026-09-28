namespace Notifications.Application.Commands.RetryAllDeliveries;

/// <summary>Outcome of <see cref="RetryAllDeliveriesCommand"/>.</summary>
/// <param name="Retried">How many dead-lettered deliveries were handed back to the retry worker.</param>
public sealed record RetryAllDeliveriesResultDto(int Retried);
