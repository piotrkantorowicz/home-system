using DietPlanner.Api.Features.Products;

namespace DietPlanner.Tests.Builders;

public class ProductBuilder
{
    private string _name = "Test Product";
    private decimal? _calories = 100;
    private decimal? _protein = 10;
    private decimal? _carbs = 10;
    private decimal? _fat = 5;
    private string _unit = "g";
    private decimal? _density = null;
    private decimal? _gramPerPiece = null;

    public ProductBuilder WithName(string name) { _name = name; return this; }
    public ProductBuilder WithCalories(decimal cal) { _calories = cal; return this; }
    public ProductBuilder WithUnit(string unit) { _unit = unit; return this; }
    public ProductBuilder WithDensity(decimal density) { _density = density; return this; }
    public ProductBuilder WithGramPerPiece(decimal g) { _gramPerPiece = g; return this; }

    public CreateProductRequest Build() =>
        new(_name, _calories, _protein, _carbs, _fat, null, _unit, _density, _gramPerPiece);
}
