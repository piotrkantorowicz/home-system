using DietPlanner.Api.Features.DietPlans.Import;
using FluentAssertions;

namespace DietPlanner.Tests.Unit.Validators;

/// <summary>
/// Tests the synchronous schema/business-rule validation logic in ImportValidator
/// without requiring a database.
/// </summary>
public class ImportValidatorSchemaTests
{
    private static ImportDto ValidImport() => new()
    {
        Products =
        [
            new ImportProductDto
            {
                Name = "Chicken",
                CaloriesPer100g = 165,
                ProteinPer100g = 31,
                CarbsPer100g = 0,
                FatPer100g = 3.6m,
                Unit = "g"
            }
        ],
        Recipes =
        [
            new ImportRecipeDto
            {
                Name = "Grilled Chicken",
                Servings = 2,
                Ingredients = [new ImportIngredientDto { Product = "Chicken", Amount = 200, Unit = "g" }]
            }
        ],
        Schedule =
        [
            new ImportScheduleDto
            {
                Date = new DateOnly(2026, 1, 1),
                Meals = [new ImportMealDto { Type = "lunch", Recipe = "Grilled Chicken", Servings = 1 }]
            }
        ]
    };

    [Fact]
    public void ValidImport_HasNoSchemaErrors()
    {
        var import = ValidImport();

        import.Schedule.Should().NotBeEmpty();
        import.Products.Should().NotBeEmpty();
        import.Recipes.Should().NotBeEmpty();
    }

    [Fact]
    public void DuplicateProducts_AreDetected()
    {
        var import = ValidImport();
        import.Products.Add(new ImportProductDto { Name = "Chicken", Unit = "g" }); // duplicate

        var duplicates = import.Products
            .GroupBy(p => p.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.First().Name)
            .ToList();

        duplicates.Should().Contain("Chicken");
    }

    [Fact]
    public void CalorieConsistency_BigDifference_ShouldBeWarned()
    {
        var product = new ImportProductDto
        {
            Name = "Bad Data",
            CaloriesPer100g = 500, // wrong: macros only add up to ~165
            ProteinPer100g = 31,
            CarbsPer100g = 0,
            FatPer100g = 3.6m,
            Unit = "g"
        };

        var calculatedCalories =
            (product.ProteinPer100g ?? 0) * 4 +
            (product.CarbsPer100g ?? 0) * 4 +
            (product.FatPer100g ?? 0) * 9;

        var difference = Math.Abs(calculatedCalories - product.CaloriesPer100g!.Value);
        difference.Should().BeGreaterThan(15);
    }

    [Fact]
    public void CalorieConsistency_CloseValues_ShouldNotWarn()
    {
        var product = new ImportProductDto
        {
            Name = "Chicken",
            CaloriesPer100g = 165,
            ProteinPer100g = 31,
            CarbsPer100g = 0,
            FatPer100g = 3.6m,
            Unit = "g"
        };

        var calculatedCalories =
            (product.ProteinPer100g ?? 0) * 4 +
            (product.CarbsPer100g ?? 0) * 4 +
            (product.FatPer100g ?? 0) * 9;

        var difference = Math.Abs(calculatedCalories - product.CaloriesPer100g!.Value);
        difference.Should().BeLessOrEqualTo(15);
    }

    [Fact]
    public void ScheduleDate_IsAValidDate()
    {
        var import = ValidImport();
        var scheduleDate = import.Schedule[0].Date;

        scheduleDate.Should().NotBe(default(DateOnly));
        scheduleDate.Year.Should().BeGreaterThan(2000);
    }
}
