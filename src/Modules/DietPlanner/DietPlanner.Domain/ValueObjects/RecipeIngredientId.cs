namespace DietPlanner.Domain.ValueObjects;

public sealed record RecipeIngredientId(Guid Value)
{
    public static RecipeIngredientId New() => new(Guid.NewGuid());
    public static RecipeIngredientId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
