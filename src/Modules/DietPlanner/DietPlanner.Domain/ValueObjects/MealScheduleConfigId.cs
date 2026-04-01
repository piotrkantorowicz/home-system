namespace DietPlanner.Domain.ValueObjects;

public sealed record MealScheduleConfigId(Guid Value)
{
    public static MealScheduleConfigId New() => new(Guid.NewGuid());
    public static MealScheduleConfigId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
