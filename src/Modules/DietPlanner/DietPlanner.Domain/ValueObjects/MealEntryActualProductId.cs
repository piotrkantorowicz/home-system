namespace DietPlanner.Domain.ValueObjects;

public sealed record MealEntryActualProductId(Guid Value)
{
    public static MealEntryActualProductId New() => new(Guid.NewGuid());
    public static MealEntryActualProductId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
