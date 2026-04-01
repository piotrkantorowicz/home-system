namespace DietPlanner.Domain.ValueObjects;

public sealed record MealSlotId(Guid Value)
{
    public static MealSlotId New() => new(Guid.NewGuid());
    public static MealSlotId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
