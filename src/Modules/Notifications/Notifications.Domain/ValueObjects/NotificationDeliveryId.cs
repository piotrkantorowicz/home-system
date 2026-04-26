namespace Notifications.Domain.ValueObjects;

public sealed record NotificationDeliveryId(Guid Value)
{
    public static NotificationDeliveryId New() => new(Guid.NewGuid());
    public static NotificationDeliveryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
