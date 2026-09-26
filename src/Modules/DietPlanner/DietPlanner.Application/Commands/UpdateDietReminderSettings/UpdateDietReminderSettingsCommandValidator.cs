namespace DietPlanner.Application.Commands.UpdateDietReminderSettings;

using Shared.Abstractions.Cqrs;

internal sealed class UpdateDietReminderSettingsCommandValidator : ICommandValidator<UpdateDietReminderSettingsCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateDietReminderSettingsCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

        if (command.MealReminderLeadTimeMinutes is < 1 or > 120)
            yield return new ValidationError(nameof(command.MealReminderLeadTimeMinutes), "MealReminderLeadTimeMinutes must be between 1 and 120.");

        if (command.MealMissedGraceMinutes is < 1 or > 240)
            yield return new ValidationError(nameof(command.MealMissedGraceMinutes), "MealMissedGraceMinutes must be between 1 and 240.");

        if (command.WaterReminderIntervalMinutes is < 15 or > 480)
            yield return new ValidationError(nameof(command.WaterReminderIntervalMinutes), "WaterReminderIntervalMinutes must be between 15 and 480.");

        if (command.WaterWindowEnd <= command.WaterWindowStart)
            yield return new ValidationError(nameof(command.WaterWindowEnd), "WaterWindowEnd must be after WaterWindowStart.");
    }
}
