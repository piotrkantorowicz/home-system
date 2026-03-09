using DietPlanner.Api.Common.Utils;
using FluentValidation;

namespace DietPlanner.Api.Features.Recipes;

/// <summary>
/// Shared validation rules for both create and update recipe requests.
/// </summary>
public abstract class BaseRecipeValidator<T> : AbstractValidator<T>
    where T : IRecipeRequest
{
    protected BaseRecipeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Recipe name is required")
            .Length(2, 200).WithMessage("Recipe name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\(\)\.,'&]+$")
            .WithMessage("Recipe name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.Instructions)
            .MaximumLength(5000).When(x => !string.IsNullOrWhiteSpace(x.Instructions))
            .WithMessage("Instructions cannot exceed 5000 characters");

        RuleFor(x => x.Servings)
            .GreaterThan(0).WithMessage("Servings must be at least 1")
            .LessThan(100).WithMessage("Servings seems unrealistic (max 100)");

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0).When(x => x.PrepTimeMinutes.HasValue)
            .WithMessage("Prep time cannot be negative")
            .LessThan(1440).When(x => x.PrepTimeMinutes.HasValue)
            .WithMessage("Prep time seems unrealistic (max 24 hours)");

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("Recipe must have at least one ingredient")
            .Must(HaveUniqueProducts).WithMessage("Recipe cannot have duplicate ingredients");

        RuleForEach(x => x.Ingredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.ProductName)
                .NotEmpty().WithMessage("Ingredient product name is required")
                .MaximumLength(200).WithMessage("Product name too long");

            ingredient.RuleFor(i => i.Amount)
                .GreaterThan(0).WithMessage("Ingredient amount must be positive");

            ingredient.RuleFor(i => i.Unit)
                .NotEmpty().WithMessage("Ingredient unit is required")
                .Must(UnitConverter.IsValidUnit)
                .WithMessage("Invalid unit. Use one of: g, kg, oz, lb, ml, l, cup, tbsp, tsp, piece");
        });
    }

    private static bool HaveUniqueProducts(List<CreateRecipeIngredientRequest> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
            return true;

        var productNames = ingredients
            .Select(i => i.ProductName.Trim().ToLowerInvariant())
            .ToList();

        return productNames.Count == productNames.Distinct().Count();
    }
}

public class CreateRecipeValidator : BaseRecipeValidator<CreateRecipeRequest> { }

public class UpdateRecipeValidator : BaseRecipeValidator<UpdateRecipeRequest> { }
