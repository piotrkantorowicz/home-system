namespace Notifications.Application.Queries.GetDeliveryContent;

/// <summary>What a delivery sends: its notification's rendered text and data.</summary>
/// <param name="DeliveryId">The delivery.</param>
/// <param name="Title">Rendered title.</param>
/// <param name="Body">Rendered body.</param>
/// <param name="Payload">The notification's data as JSON.</param>
public sealed record DeliveryContentDto(Guid DeliveryId, string Title, string Body, string Payload);
