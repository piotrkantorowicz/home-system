namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class RecipeTests
{
    [Fact]
    public void Create_WithValidData_CreatesRecipe()
    {
        var id = RecipeId.New();

        var recipe = Recipe.Create(id, "Pasta", "Tasty pasta", null, 2, 20, "user-1");

        recipe.Id.ShouldBe(id);
        recipe.Name.ShouldBe("Pasta");
        recipe.Description.ShouldBe("Tasty pasta");
        recipe.Servings.ShouldBe(2);
        recipe.PrepTimeMinutes.ShouldBe(20);
        recipe.Ingredients.ShouldBeEmpty();
        recipe.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void AddIngredient_AddsToCollection()
    {
        var recipe = Recipe.Create(RecipeId.New(), "Pasta", null, null, 2, null, "user-1");
        var productId = ProductId.New();

        recipe.AddIngredient(RecipeIngredientId.New(), productId, 200m, "g");

        recipe.Ingredients.ShouldHaveSingleItem();
        recipe.Ingredients.First().ProductId.ShouldBe(productId);
        recipe.Ingredients.First().Amount.ShouldBe(200m);
    }

    [Fact]
    public void ClearIngredients_RemovesAll()
    {
        var recipe = Recipe.Create(RecipeId.New(), "Pasta", null, null, 2, null, "user-1");
        recipe.AddIngredient(RecipeIngredientId.New(), ProductId.New(), 100m, "g");
        recipe.AddIngredient(RecipeIngredientId.New(), ProductId.New(), 50m, "ml");

        recipe.ClearIngredients();

        recipe.Ingredients.ShouldBeEmpty();
    }

    [Fact]
    public void Update_WithValidData_UpdatesRecipe()
    {
        var recipe = Recipe.Create(RecipeId.New(), "Old", null, null, 1, null, "user-1");

        recipe.Update("New", "desc", "instructions", 4, 30);

        recipe.Name.ShouldBe("New");
        recipe.Description.ShouldBe("desc");
        recipe.Servings.ShouldBe(4);
        recipe.PrepTimeMinutes.ShouldBe(30);
        recipe.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SoftDelete_WhenNotDeleted_SetsDeletedAt()
    {
        var recipe = Recipe.Create(RecipeId.New(), "Pasta", null, null, 2, null, "user-1");

        recipe.SoftDelete();

        recipe.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ThrowsDomainException()
    {
        var recipe = Recipe.Create(RecipeId.New(), "Pasta", null, null, 2, null, "user-1");
        recipe.SoftDelete();

        var act = () => recipe.SoftDelete();

        act.ShouldThrow<DietPlannerDomainException>();
    }
}
