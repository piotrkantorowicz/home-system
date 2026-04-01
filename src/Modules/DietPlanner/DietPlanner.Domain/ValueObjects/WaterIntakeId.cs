namespace DietPlanner.Domain.ValueObjects;

public sealed record WaterIntakeId(Guid Value)
{
    public static WaterIntakeId New() => new(Guid.NewGuid());
    public static WaterIntakeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
