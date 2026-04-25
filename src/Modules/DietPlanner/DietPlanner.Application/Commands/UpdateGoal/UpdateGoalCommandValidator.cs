namespace DietPlanner.Application.Commands.UpdateGoal;

using Shared.Abstractions.Cqrs;

internal sealed class UpdateGoalCommandValidator : ICommandValidator<UpdateGoalCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateGoalCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.DailyCalorieTarget is < 0)
            yield return new ValidationError(nameof(command.DailyCalorieTarget), "DailyCalorieTarget must be non-negative.");

        if (command.ProteinGrams is < 0)
            yield return new ValidationError(nameof(command.ProteinGrams), "ProteinGrams must be non-negative.");

        if (command.CarbsGrams is < 0)
            yield return new ValidationError(nameof(command.CarbsGrams), "CarbsGrams must be non-negative.");

        if (command.FatGrams is < 0)
            yield return new ValidationError(nameof(command.FatGrams), "FatGrams must be non-negative.");

        if (command.FiberGrams is < 0)
            yield return new ValidationError(nameof(command.FiberGrams), "FiberGrams must be non-negative.");
    }
}
