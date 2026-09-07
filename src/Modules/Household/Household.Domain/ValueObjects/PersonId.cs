namespace Household.Domain.ValueObjects;

public sealed record PersonId(Guid Value)
{
    public static PersonId New() => new(Guid.NewGuid());
    public static PersonId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
