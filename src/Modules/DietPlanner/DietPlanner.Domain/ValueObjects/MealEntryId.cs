namespace DietPlanner.Domain.ValueObjects;

public sealed record MealEntryId(Guid Value)
{
    public static MealEntryId New() => new(Guid.NewGuid());
    public static MealEntryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
