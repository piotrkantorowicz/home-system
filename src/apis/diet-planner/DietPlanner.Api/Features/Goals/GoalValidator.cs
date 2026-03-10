using FluentValidation;

namespace DietPlanner.Api.Features.Goals;

public abstract class BaseGoalValidator<T> : AbstractValidator<T> where T : IGoalRequest
{
    protected BaseGoalValidator()
    {
        RuleFor(x => x.DailyCalorieTarget)
            .InclusiveBetween(0, 20000)
            .When(x => x.DailyCalorieTarget.HasValue);

        RuleFor(x => x.ProteinGrams)
            .InclusiveBetween(0, 1000)
            .When(x => x.ProteinGrams.HasValue);

        RuleFor(x => x.CarbsGrams)
            .InclusiveBetween(0, 1000)
            .When(x => x.CarbsGrams.HasValue);

        RuleFor(x => x.FatGrams)
            .InclusiveBetween(0, 1000)
            .When(x => x.FatGrams.HasValue);

        RuleFor(x => x.FiberGrams)
            .InclusiveBetween(0, 200)
            .When(x => x.FiberGrams.HasValue);
    }
}

public class CreateGoalValidator : BaseGoalValidator<CreateGoalRequest> { }
public class UpdateGoalValidator : BaseGoalValidator<UpdateGoalRequest> { }
