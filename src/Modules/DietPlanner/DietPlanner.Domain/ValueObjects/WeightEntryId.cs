namespace DietPlanner.Domain.ValueObjects;

public sealed record WeightEntryId(Guid Value)
{
    public static WeightEntryId New() => new(Guid.NewGuid());
    public static WeightEntryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
