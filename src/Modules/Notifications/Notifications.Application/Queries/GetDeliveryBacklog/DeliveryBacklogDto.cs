namespace Notifications.Application.Queries.GetDeliveryBacklog;

/// <summary>
/// Failed notification deliveries.
/// </summary>
/// <param name="DeadLettered">Failed deliveries that used up their attempts; only a retry moves them.</param>
/// <param name="Retrying">Failed deliveries the retry worker will still try again.</param>
public sealed record DeliveryBacklogDto(int DeadLettered, int Retrying);
