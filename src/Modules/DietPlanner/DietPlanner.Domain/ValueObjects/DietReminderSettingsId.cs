namespace DietPlanner.Domain.ValueObjects;

public sealed record DietReminderSettingsId(Guid Value)
{
    public static DietReminderSettingsId New() => new(Guid.NewGuid());
    public static DietReminderSettingsId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
