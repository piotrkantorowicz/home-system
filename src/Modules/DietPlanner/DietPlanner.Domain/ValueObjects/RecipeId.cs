namespace DietPlanner.Domain.ValueObjects;

public sealed record RecipeId(Guid Value)
{
    public static RecipeId New() => new(Guid.NewGuid());
    public static RecipeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
