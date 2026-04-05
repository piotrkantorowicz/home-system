namespace DietPlanner.Domain.ValueObjects;

public sealed record HydrationConfigId(Guid Value)
{
    public static HydrationConfigId New() => new(Guid.NewGuid());
    public static HydrationConfigId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
