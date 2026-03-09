using DietPlanner.Api.Domain.Constants;
using FluentValidation;

namespace DietPlanner.Api.Features.DietPlans;

public abstract class BaseMealEntryValidator<T> : AbstractValidator<T>
    where T : IMealEntryRequest
{
    protected BaseMealEntryValidator()
    {
        RuleFor(x => x.MealType)
            .NotEmpty().WithMessage("Meal type is required")
            .Must(MealType.IsValid)
            .WithMessage($"Invalid meal type. Use one of: {string.Join(", ", MealType.All)}");

        RuleFor(x => x.RecipeId)
            .NotEmpty().WithMessage("Recipe is required");

        RuleFor(x => x.Servings)
            .GreaterThan(0).WithMessage("Servings must be greater than 0")
            .LessThanOrEqualTo(50).WithMessage("Servings cannot exceed 50");
    }
}

public class CreateMealEntryValidator : BaseMealEntryValidator<CreateMealEntryRequest> { }

public class UpdateMealEntryValidator : BaseMealEntryValidator<UpdateMealEntryRequest> { }

public class CreateDietPlanValidator : AbstractValidator<CreateDietPlanRequest>
{
    public CreateDietPlanValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required")
            .MaximumLength(200).WithMessage("Plan name cannot exceed 200 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date");
    }
}
