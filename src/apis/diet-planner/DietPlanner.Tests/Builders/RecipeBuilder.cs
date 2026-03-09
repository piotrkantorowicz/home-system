using DietPlanner.Api.Features.Recipes;

namespace DietPlanner.Tests.Builders;

public class RecipeBuilder
{
    private string _name = "Test Recipe";
    private int _servings = 1;
    private string? _description = null;
    private List<CreateRecipeIngredientRequest> _ingredients = [];

    public RecipeBuilder WithName(string name) { _name = name; return this; }
    public RecipeBuilder WithServings(int s) { _servings = s; return this; }
    public RecipeBuilder WithDescription(string d) { _description = d; return this; }
    public RecipeBuilder WithIngredient(string productName, decimal amount, string unit = "g")
    {
        _ingredients.Add(new CreateRecipeIngredientRequest(productName, amount, unit));
        return this;
    }

    public CreateRecipeRequest Build() =>
        new(_name, _description, null, _servings, null, _ingredients);
}
