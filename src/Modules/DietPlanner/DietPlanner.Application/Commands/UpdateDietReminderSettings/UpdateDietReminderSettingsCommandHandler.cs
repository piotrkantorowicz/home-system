namespace DietPlanner.Application.Commands.UpdateDietReminderSettings;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class UpdateDietReminderSettingsCommandHandler : ICommandHandler<UpdateDietReminderSettingsCommand>
{
    private readonly IDietReminderSettingsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDietReminderSettingsCommandHandler(
        IDietReminderSettingsRepository repository,
        IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateDietReminderSettingsCommand command, CancellationToken ct = default)
    {
        var settings = await _repository.GetByUserIdAsync(command.UserId, ct);

        if (settings is null)
        {
            settings = DietReminderSettings.Create(
                DietReminderSettingsId.New(),
                command.UserId,
                command.MealRemindersEnabled,
                command.MealReminderLeadTimeMinutes,
                command.MealMissedGraceMinutes,
                command.WaterRemindersEnabled,
                command.WaterReminderIntervalMinutes,
                command.WaterWindowStartUtc,
                command.WaterWindowEndUtc,
                command.WeeklySummaryEnabled,
                command.WeeklySummaryDayOfWeekUtc,
                command.WeeklySummaryTimeOfDayUtc,
                command.GoalAlertsEnabled);

            await _repository.AddAsync(settings, ct);
        }
        else
        {
            settings.Update(
                command.MealRemindersEnabled,
                command.MealReminderLeadTimeMinutes,
                command.MealMissedGraceMinutes,
                command.WaterRemindersEnabled,
                command.WaterReminderIntervalMinutes,
                command.WaterWindowStartUtc,
                command.WaterWindowEndUtc,
                command.WeeklySummaryEnabled,
                command.WeeklySummaryDayOfWeekUtc,
                command.WeeklySummaryTimeOfDayUtc,
                command.GoalAlertsEnabled);

            _repository.Update(settings);
        }

        await _unitOfWork.CommitAsync(ct);
    }
}
