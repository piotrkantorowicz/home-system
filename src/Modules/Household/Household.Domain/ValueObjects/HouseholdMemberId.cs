namespace Household.Domain.ValueObjects;

public sealed record HouseholdMemberId(Guid Value)
{
    public static HouseholdMemberId New() => new(Guid.NewGuid());
    public static HouseholdMemberId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
