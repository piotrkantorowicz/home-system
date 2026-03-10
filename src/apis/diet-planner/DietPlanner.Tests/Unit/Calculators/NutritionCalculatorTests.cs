using DietPlanner.Api.Domain;
using DietPlanner.Api.Features.Recipes;
using FluentAssertions;

namespace DietPlanner.Tests.Unit.Calculators;

public class NutritionCalculatorTests
{
    private readonly NutritionCalculator _sut = new();

    private static Product MakeProduct(
        decimal? calories = null,
        decimal? protein = null,
        decimal? carbs = null,
        decimal? fat = null,
        decimal? fiber = null,
        decimal? density = null,
        decimal? gramPerPiece = null) =>
        new()
        {
            Name = "Test",
            CreatedByUserId = "user",
            CaloriesPer100g = calories,
            ProteinPer100g = protein,
            CarbsPer100g = carbs,
            FatPer100g = fat,
            FiberPer100g = fiber,
            DensityGramsPerMl = density,
            GramPerPiece = gramPerPiece
        };

    private static Recipe MakeRecipe(int servings = 1, params RecipeIngredient[] ingredients)
    {
        var recipe = new Recipe { Name = "Test", CreatedByUserId = "user", Servings = servings };
        foreach (var i in ingredients)
            recipe.Ingredients.Add(i);
        return recipe;
    }

    private static RecipeIngredient Ingredient(Product product, decimal amount, string unit) =>
        new() { Amount = amount, Unit = unit, Product = product };

    // ============================================================
    // CalculateTotalNutrition
    // ============================================================

    [Fact]
    public void CalculateTotalNutrition_EmptyIngredients_ReturnsZeros()
    {
        var recipe = MakeRecipe();

        var result = _sut.CalculateTotalNutrition(recipe);

        result.Calories.Should().Be(0);
        result.Protein.Should().Be(0);
        result.Carbs.Should().Be(0);
        result.Fat.Should().Be(0);
        result.Fiber.Should().Be(0);
    }

    [Fact]
    public void CalculateTotalNutrition_SingleIngredient100g_ReturnsPer100gValues()
    {
        var product = MakeProduct(calories: 200, protein: 20, carbs: 10, fat: 8, fiber: 3);
        var recipe = MakeRecipe(1, Ingredient(product, 100, "g"));

        var result = _sut.CalculateTotalNutrition(recipe);

        result.Calories.Should().Be(200);
        result.Protein.Should().Be(20);
        result.Carbs.Should().Be(10);
        result.Fat.Should().Be(8);
        result.Fiber.Should().Be(3);
    }

    [Fact]
    public void CalculateTotalNutrition_ProductWithFiber_CalculatesFiberCorrectly()
    {
        var product = MakeProduct(calories: 389, protein: 17, carbs: 66, fat: 7, fiber: 10.6m);
        var recipe = MakeRecipe(1, Ingredient(product, 50, "g")); // half portion

        var result = _sut.CalculateTotalNutrition(recipe);

        result.Fiber.Should().Be(5.3m);
    }

    [Fact]
    public void CalculateTotalNutrition_SingleIngredient200g_DoublesValues()
    {
        var product = MakeProduct(calories: 100, protein: 10, carbs: 5, fat: 4);
        var recipe = MakeRecipe(1, Ingredient(product, 200, "g"));

        var result = _sut.CalculateTotalNutrition(recipe);

        result.Calories.Should().Be(200);
        result.Protein.Should().Be(20);
    }

    [Fact]
    public void CalculateTotalNutrition_KgUnit_ConvertsCorrectly()
    {
        var product = MakeProduct(calories: 100);
        var recipe = MakeRecipe(1, Ingredient(product, 0.5m, "kg")); // 500g

        var result = _sut.CalculateTotalNutrition(recipe);

        result.Calories.Should().Be(500);
    }

    [Fact]
    public void CalculateTotalNutrition_VolumeUnitWithDensity_ConvertsCorrectly()
    {
        // Olive oil: 884 kcal/100g, density 0.92 g/ml
        var oil = MakeProduct(calories: 884, density: 0.92m);
        var recipe = MakeRecipe(1, Ingredient(oil, 100, "ml")); // 92g of oil

        var result = _sut.CalculateTotalNutrition(recipe);

        // 92g * (884/100) = 813.28 kcal
        result.Calories.Should().BeApproximately(813.3m, 0.5m);
    }

    [Fact]
    public void CalculateTotalNutrition_NullProductSkipped_NoException()
    {
        var ingredient = new RecipeIngredient { Amount = 100, Unit = "g", Product = null! };
        var recipe = MakeRecipe();
        recipe.Ingredients.Add(ingredient);

        var act = () => _sut.CalculateTotalNutrition(recipe);

        act.Should().NotThrow();
    }

    [Fact]
    public void CalculateTotalNutrition_NullableNutritionDefaults_TreatsAsZero()
    {
        var product = MakeProduct(calories: null, protein: null);
        var recipe = MakeRecipe(1, Ingredient(product, 100, "g"));

        var result = _sut.CalculateTotalNutrition(recipe);

        result.Calories.Should().Be(0);
        result.Protein.Should().Be(0);
    }

    // ============================================================
    // CalculateNutritionPerServing
    // ============================================================

    [Fact]
    public void CalculateNutritionPerServing_TwoServings_HalvesTotals()
    {
        // 200g of 400kcal/100g product = 800kcal total, divided by 2 servings = 400kcal
        var product = MakeProduct(calories: 400, protein: 40);
        var recipe = MakeRecipe(servings: 2, Ingredient(product, 200, "g"));

        var result = _sut.CalculateNutritionPerServing(recipe);

        result.Calories.Should().Be(400);
        result.Protein.Should().Be(40);
    }

    [Fact]
    public void CalculateNutritionPerServing_ZeroServings_TreatsAsOneServing()
    {
        var product = MakeProduct(calories: 200);
        var recipe = MakeRecipe(servings: 0, Ingredient(product, 100, "g"));

        var result = _sut.CalculateNutritionPerServing(recipe);

        result.Calories.Should().Be(200); // divide by max(0,1) = 1
    }

    // ============================================================
    // CalculateNutritionForServings
    // ============================================================

    [Fact]
    public void CalculateNutritionForServings_TwoServings_DoublesPerServing()
    {
        var product = MakeProduct(calories: 200, protein: 20);
        var recipe = MakeRecipe(servings: 1, Ingredient(product, 100, "g"));

        var result = _sut.CalculateNutritionForServings(recipe, 2);

        result.Calories.Should().Be(400);
        result.Protein.Should().Be(40);
    }

    [Fact]
    public void CalculateNutritionForServings_FractionalServings_CalculatesCorrectly()
    {
        var product = MakeProduct(calories: 200);
        var recipe = MakeRecipe(servings: 1, Ingredient(product, 100, "g"));

        var result = _sut.CalculateNutritionForServings(recipe, 0.5m);

        result.Calories.Should().Be(100);
    }
}
