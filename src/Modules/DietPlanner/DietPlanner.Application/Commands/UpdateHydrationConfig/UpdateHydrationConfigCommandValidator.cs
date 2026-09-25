namespace DietPlanner.Application.Commands.UpdateHydrationConfig;

using Shared.Abstractions.Cqrs;

internal sealed class UpdateHydrationConfigCommandValidator : ICommandValidator<UpdateHydrationConfigCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateHydrationConfigCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

        if (command.DailyWaterTargetMl is < 100 or > 10000)
            yield return new ValidationError(nameof(command.DailyWaterTargetMl), "DailyWaterTargetMl must be between 100 and 10000.");

        if (command.GlassSizeMl is < 50 or > 2000)
            yield return new ValidationError(nameof(command.GlassSizeMl), "GlassSizeMl must be between 50 and 2000.");
    }
}
