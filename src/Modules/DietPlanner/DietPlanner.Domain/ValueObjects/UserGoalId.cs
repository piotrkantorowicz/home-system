namespace DietPlanner.Domain.ValueObjects;

public sealed record UserGoalId(Guid Value)
{
    public static UserGoalId New() => new(Guid.NewGuid());
    public static UserGoalId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
