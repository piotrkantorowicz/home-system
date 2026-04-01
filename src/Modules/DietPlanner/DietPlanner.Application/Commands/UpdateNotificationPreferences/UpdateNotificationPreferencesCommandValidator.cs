namespace DietPlanner.Application.Commands.UpdateNotificationPreferences;

using Shared.Abstractions.CQRS;

internal sealed class UpdateNotificationPreferencesCommandValidator : ICommandValidator<UpdateNotificationPreferencesCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateNotificationPreferencesCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
            yield return new ValidationError(nameof(command.UserId), "UserId is required.");

        if (command.MealReminderLeadTimeMinutes is < 1 or > 120)
            yield return new ValidationError(nameof(command.MealReminderLeadTimeMinutes), "MealReminderLeadTimeMinutes must be between 1 and 120.");

        if (command.WaterReminderIntervalMinutes is < 15 or > 480)
            yield return new ValidationError(nameof(command.WaterReminderIntervalMinutes), "WaterReminderIntervalMinutes must be between 15 and 480.");
    }
}
