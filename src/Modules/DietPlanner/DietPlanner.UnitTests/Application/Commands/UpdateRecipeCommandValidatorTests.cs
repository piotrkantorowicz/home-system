namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Application.Commands.UpdateRecipe;

/// <summary>Unit tests for <c>UpdateRecipeCommandValidator</c>: ingredient lines follow the same rules as on create.</summary>
public sealed class UpdateRecipeCommandValidatorTests
{
    private readonly UpdateRecipeCommandValidator _sut = new();

    /// <summary>Non-positive amounts and unsupported units are rejected per line.</summary>
    [Theory]
    [InlineData(0, "g", "Ingredients[0].Amount")]
    [InlineData(-5, "g", "Ingredients[0].Amount")]
    [InlineData(10, "handful", "Ingredients[0].Unit")]
    public void Validate_WithInvalidIngredient_ReportsLine(int amount, string unit, string property)
    {
        var errors = _sut.Validate(Command([new CreateRecipeIngredientRequest(Guid.CreateVersion7(), amount, unit)]));

        errors.ShouldHaveSingleItem().PropertyName.ShouldBe(property);
    }

    /// <summary>More than the allowed number of lines is rejected.</summary>
    [Fact]
    public void Validate_WithTooManyIngredients_ReportsIngredients()
    {
        var lines = Enumerable.Range(0, RecipeIngredientRules.MaxIngredients + 1)
            .Select(_ => new CreateRecipeIngredientRequest(Guid.CreateVersion7(), 10m, "g"))
            .ToList();

        _sut.Validate(Command(lines)).ShouldHaveSingleItem().PropertyName.ShouldBe("Ingredients");
    }

    /// <summary>An update cannot empty the ingredient list, which create would reject.</summary>
    [Fact]
    public void Validate_WithNoIngredients_ReportsIngredients()
        => _sut.Validate(Command([])).ShouldHaveSingleItem().PropertyName.ShouldBe("Ingredients");

    private static UpdateRecipeCommand Command(IReadOnlyList<CreateRecipeIngredientRequest> ingredients)
        => new(Guid.CreateVersion7(), "Soup", null, null, 1, null, ingredients, null, "user-1");
}
