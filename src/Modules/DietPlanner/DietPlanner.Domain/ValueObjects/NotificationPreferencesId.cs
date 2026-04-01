namespace DietPlanner.Domain.ValueObjects;

public sealed record NotificationPreferencesId(Guid Value)
{
    public static NotificationPreferencesId New() => new(Guid.NewGuid());
    public static NotificationPreferencesId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
