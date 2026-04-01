namespace DietPlanner.Domain.ValueObjects;

public sealed record UserProfileId(Guid Value)
{
    public static UserProfileId New() => new(Guid.NewGuid());
    public static UserProfileId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
