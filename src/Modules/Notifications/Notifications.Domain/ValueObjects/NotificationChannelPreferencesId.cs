namespace Notifications.Domain.ValueObjects;

public sealed record NotificationChannelPreferencesId(Guid Value)
{
    public static NotificationChannelPreferencesId New() => new(Guid.NewGuid());
    public static NotificationChannelPreferencesId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
