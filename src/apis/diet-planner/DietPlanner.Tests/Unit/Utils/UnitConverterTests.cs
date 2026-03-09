using DietPlanner.Api.Common.Utils;
using DietPlanner.Api.Domain;
using FluentAssertions;

namespace DietPlanner.Tests.Unit.Utils;

public class UnitConverterTests
{
    private static Product WaterProduct() => new()
    {
        Name = "Water",
        CreatedByUserId = "user",
        DensityGramsPerMl = 1.0m
    };

    private static Product OliveOilProduct() => new()
    {
        Name = "Olive Oil",
        CreatedByUserId = "user",
        DensityGramsPerMl = 0.92m
    };

    private static Product EggProduct() => new()
    {
        Name = "Egg",
        CreatedByUserId = "user",
        GramPerPiece = 60m
    };

    // ============================================================
    // Weight units
    // ============================================================

    [Theory]
    [InlineData(100, "g", 100)]
    [InlineData(1, "kg", 1000)]
    [InlineData(1, "oz", 28.3495)]
    [InlineData(1, "lb", 453.592)]
    public void ConvertToGrams_WeightUnits_AreCorrect(decimal amount, string unit, double expectedGrams)
    {
        var result = UnitConverter.ConvertToGrams(amount, unit, WaterProduct());

        result.Should().BeApproximately((decimal)expectedGrams, 0.01m);
    }

    // ============================================================
    // Volume units
    // ============================================================

    [Theory]
    [InlineData(100, "ml", 1.0, 100)]
    [InlineData(1, "l", 1.0, 1000)]
    [InlineData(1, "cup", 1.0, 240)]
    [InlineData(1, "tbsp", 1.0, 15)]
    [InlineData(1, "tsp", 1.0, 5)]
    public void ConvertToGrams_VolumeUnitsWithWater_AreCorrect(
        decimal amount, string unit, double density, double expectedGrams)
    {
        var product = new Product { Name = "P", CreatedByUserId = "u", DensityGramsPerMl = (decimal)density };

        var result = UnitConverter.ConvertToGrams(amount, unit, product);

        result.Should().BeApproximately((decimal)expectedGrams, 0.01m);
    }

    [Fact]
    public void ConvertToGrams_VolumeWithOilDensity_UsesDensity()
    {
        // 100ml olive oil * 0.92 g/ml = 92g
        var result = UnitConverter.ConvertToGrams(100, "ml", OliveOilProduct());

        result.Should().BeApproximately(92m, 0.01m);
    }

    [Fact]
    public void ConvertToGrams_VolumeWithNullDensity_DefaultsToWater()
    {
        var product = new Product { Name = "P", CreatedByUserId = "u" }; // no density

        var result = UnitConverter.ConvertToGrams(100, "ml", product);

        result.Should().Be(100); // defaults to 1.0 g/ml
    }

    // ============================================================
    // Piece units
    // ============================================================

    [Fact]
    public void ConvertToGrams_PieceWithGramPerPiece_MultipliesCorrectly()
    {
        var result = UnitConverter.ConvertToGrams(2, "piece", EggProduct());

        result.Should().Be(120); // 2 * 60g
    }

    [Fact]
    public void ConvertToGrams_PieceWithNullGramPerPiece_FallsBackTo100g()
    {
        var product = new Product { Name = "P", CreatedByUserId = "u" }; // no gramPerPiece

        var result = UnitConverter.ConvertToGrams(1, "piece", product);

        result.Should().Be(100);
    }

    // ============================================================
    // IsValidUnit
    // ============================================================

    [Theory]
    [InlineData("g", true)]
    [InlineData("kg", true)]
    [InlineData("oz", true)]
    [InlineData("lb", true)]
    [InlineData("ml", true)]
    [InlineData("l", true)]
    [InlineData("cup", true)]
    [InlineData("tbsp", true)]
    [InlineData("tsp", true)]
    [InlineData("piece", true)]
    [InlineData("xyz", false)]
    [InlineData("", false)]
    public void IsValidUnit_KnownUnits_ReturnsExpected(string unit, bool expected)
    {
        UnitConverter.IsValidUnit(unit).Should().Be(expected);
    }
}
