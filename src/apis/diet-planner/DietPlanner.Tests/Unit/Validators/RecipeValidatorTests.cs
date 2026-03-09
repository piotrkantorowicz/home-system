using DietPlanner.Api.Features.Recipes;
using FluentValidation.TestHelper;

namespace DietPlanner.Tests.Unit.Validators;

public class CreateRecipeValidatorTests
{
    private readonly CreateRecipeValidator _sut = new();

    private static CreateRecipeRequest ValidRequest() =>
        new("Omelette", null, null, 1, null,
            [new CreateRecipeIngredientRequest("Eggs", 2, "piece")]);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _sut.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Name_Empty_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { Name = "" });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Servings_Zero_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { Servings = 0 });
        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Ingredients_Empty_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { Ingredients = [] });
        result.ShouldHaveValidationErrorFor(x => x.Ingredients);
    }

    [Fact]
    public void Ingredients_DuplicateProduct_HasError()
    {
        var dupes = new List<CreateRecipeIngredientRequest>
        {
            new("Eggs", 2, "piece"),
            new("eggs", 1, "piece") // same product, different case
        };

        var result = _sut.TestValidate(ValidRequest() with { Ingredients = dupes });
        result.ShouldHaveValidationErrorFor(x => x.Ingredients);
    }

    [Fact]
    public void Ingredient_NegativeAmount_HasError()
    {
        var ingredients = new List<CreateRecipeIngredientRequest>
        {
            new("Eggs", -1, "piece")
        };

        var result = _sut.TestValidate(ValidRequest() with { Ingredients = ingredients });
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Ingredient_InvalidUnit_HasError()
    {
        var ingredients = new List<CreateRecipeIngredientRequest>
        {
            new("Eggs", 2, "scoop")
        };

        var result = _sut.TestValidate(ValidRequest() with { Ingredients = ingredients });
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Description_TooLong_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { Description = new string('A', 1001) });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
