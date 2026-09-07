namespace Household.Domain.ValueObjects;

public sealed record HouseholdId(Guid Value)
{
    public static HouseholdId New() => new(Guid.NewGuid());
    public static HouseholdId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
