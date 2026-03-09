using DietPlanner.Api.Features.Products;
using FluentValidation.TestHelper;

namespace DietPlanner.Tests.Unit.Validators;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _sut = new();

    private static CreateProductRequest ValidRequest() =>
        new("Chicken Breast", 165, 31, 0, 3.6m, "g", null, null);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _sut.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("A")]   // too short
    public void Name_Invalid_HasError(string name)
    {
        var result = _sut.TestValidate(ValidRequest() with { Name = name });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { Name = new string('A', 201) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Calories_Negative_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { CaloriesPer100g = -1 });
        result.ShouldHaveValidationErrorFor(x => x.CaloriesPer100g);
    }

    [Fact]
    public void Calories_Over9000_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { CaloriesPer100g = 9000 });
        result.ShouldHaveValidationErrorFor(x => x.CaloriesPer100g);
    }

    [Fact]
    public void Protein_Over100_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { ProteinPer100g = 101 });
        result.ShouldHaveValidationErrorFor(x => x.ProteinPer100g);
    }

    [Fact]
    public void InvalidUnit_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { DefaultUnit = "spoon" });
        result.ShouldHaveValidationErrorFor(x => x.DefaultUnit);
    }

    [Fact]
    public void Density_Zero_HasError()
    {
        var result = _sut.TestValidate(ValidRequest() with { DensityGramsPerMl = 0 });
        result.ShouldHaveValidationErrorFor(x => x.DensityGramsPerMl);
    }

    [Fact]
    public void Density_Null_Passes()
    {
        var result = _sut.TestValidate(ValidRequest() with { DensityGramsPerMl = null });
        result.ShouldNotHaveValidationErrorFor(x => x.DensityGramsPerMl);
    }
}

public class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _sut = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new UpdateProductRequest("Eggs", 155, 13, 1.1m, 11m, "piece", null, 60);
        var result = _sut.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Name_Empty_HasError()
    {
        var request = new UpdateProductRequest("", 100, null, null, null);
        var result = _sut.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
