namespace Notifications.Application.Queries.GetDeliveryContent;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin view: the notification a delivery carries (title, body, JSON payload), to diagnose a
/// dead letter. May contain personal data.
/// </summary>
/// <param name="DeliveryId">The delivery to inspect.</param>
public sealed record GetDeliveryContentQuery(Guid DeliveryId) : IQuery<DeliveryContentDto?>;
