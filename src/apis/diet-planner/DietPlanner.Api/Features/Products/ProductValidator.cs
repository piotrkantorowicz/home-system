using DietPlanner.Api.Common.Utils;
using FluentValidation;

namespace DietPlanner.Api.Features.Products;

/// <summary>
/// Shared validation rules for both create and update product requests.
/// </summary>
public abstract class BaseProductValidator<T> : AbstractValidator<T>
    where T : IProductRequest
{
    protected BaseProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .Length(2, 200).WithMessage("Product name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\(\)\.,'&]+$")
            .WithMessage("Product name contains invalid characters");

        RuleFor(x => x.CaloriesPer100g)
            .GreaterThanOrEqualTo(0).When(x => x.CaloriesPer100g.HasValue)
            .WithMessage("Calories cannot be negative")
            .LessThan(9000).When(x => x.CaloriesPer100g.HasValue)
            .WithMessage("Calories per 100g seems unrealistic (max 9000)");

        RuleFor(x => x.ProteinPer100g)
            .GreaterThanOrEqualTo(0).When(x => x.ProteinPer100g.HasValue)
            .WithMessage("Protein cannot be negative")
            .LessThanOrEqualTo(100).When(x => x.ProteinPer100g.HasValue)
            .WithMessage("Protein per 100g cannot exceed 100g");

        RuleFor(x => x.CarbsPer100g)
            .GreaterThanOrEqualTo(0).When(x => x.CarbsPer100g.HasValue)
            .WithMessage("Carbs cannot be negative")
            .LessThanOrEqualTo(100).When(x => x.CarbsPer100g.HasValue)
            .WithMessage("Carbs per 100g cannot exceed 100g");

        RuleFor(x => x.FatPer100g)
            .GreaterThanOrEqualTo(0).When(x => x.FatPer100g.HasValue)
            .WithMessage("Fat cannot be negative")
            .LessThanOrEqualTo(100).When(x => x.FatPer100g.HasValue)
            .WithMessage("Fat per 100g cannot exceed 100g");

        RuleFor(x => x.DefaultUnit)
            .NotEmpty().WithMessage("Default unit is required")
            .Must(UnitConverter.IsValidUnit)
            .WithMessage("Invalid unit. Use one of: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece");

        RuleFor(x => x.DensityGramsPerMl)
            .GreaterThan(0).When(x => x.DensityGramsPerMl.HasValue)
            .WithMessage("Density must be positive")
            .LessThan(25).When(x => x.DensityGramsPerMl.HasValue)
            .WithMessage("Density seems unrealistic (max 25 g/ml)");

        RuleFor(x => x.GramPerPiece)
            .GreaterThan(0).When(x => x.GramPerPiece.HasValue)
            .WithMessage("Grams per piece must be positive")
            .LessThan(10000).When(x => x.GramPerPiece.HasValue)
            .WithMessage("Grams per piece seems unrealistic (max 10kg)");
    }
}

public class CreateProductValidator : BaseProductValidator<CreateProductRequest> { }

public class UpdateProductValidator : BaseProductValidator<UpdateProductRequest> { }
