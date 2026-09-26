namespace DietPlanner.Application.Commands.UpdateDietReminderSettings;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateDietReminderSettingsCommandHandler(
    IDietReminderSettingsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateDietReminderSettingsCommand>
{
    public async Task HandleAsync(UpdateDietReminderSettingsCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var settings = await repository.GetByPersonIdAsync(command.PersonId, ct);

        if (settings is null)
        {
            settings = DietReminderSettings.Create(
                DietReminderSettingsId.New(),
                command.PersonId,
                now,
                command.MealRemindersEnabled,
                command.MealReminderLeadTimeMinutes,
                command.MealMissedGraceMinutes,
                command.WaterRemindersEnabled,
                command.WaterReminderIntervalMinutes,
                command.WaterWindowStart,
                command.WaterWindowEnd,
                command.WeeklySummaryEnabled,
                command.WeeklySummaryDayOfWeek,
                command.WeeklySummaryTimeOfDay,
                command.GoalAlertsEnabled);

            await repository.AddAsync(settings, ct);
        }
        else
        {
            settings.Update(
                command.MealRemindersEnabled,
                command.MealReminderLeadTimeMinutes,
                command.MealMissedGraceMinutes,
                command.WaterRemindersEnabled,
                command.WaterReminderIntervalMinutes,
                command.WaterWindowStart,
                command.WaterWindowEnd,
                command.WeeklySummaryEnabled,
                command.WeeklySummaryDayOfWeek,
                command.WeeklySummaryTimeOfDay,
                command.GoalAlertsEnabled,
                now);

            repository.Update(settings);
        }

        await unitOfWork.CommitAsync(ct);
    }
}
