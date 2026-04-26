namespace Notifications.Domain.ValueObjects;

public sealed record NotificationId(Guid Value)
{
    public static NotificationId New() => new(Guid.NewGuid());
    public static NotificationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
