namespace DietPlanner.Application.Commands.UpdateNotificationPreferences;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class UpdateNotificationPreferencesCommandHandler : ICommandHandler<UpdateNotificationPreferencesCommand>
{
    private readonly INotificationPreferencesRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationPreferencesCommandHandler(
        INotificationPreferencesRepository repository,
        IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateNotificationPreferencesCommand command, CancellationToken ct = default)
    {
        var preferences = await _repository.GetByUserIdAsync(command.UserId, ct);

        if (preferences is null)
        {
            preferences = NotificationPreferences.Create(
                NotificationPreferencesId.New(),
                command.UserId,
                command.MealReminderEnabled,
                command.MealReminderLeadTimeMinutes,
                command.WaterReminderEnabled,
                command.WaterReminderIntervalMinutes,
                command.WeeklySummaryEnabled,
                command.GoalMilestoneAlertsEnabled);

            await _repository.AddAsync(preferences, ct);
        }
        else
        {
            preferences.Update(
                command.MealReminderEnabled,
                command.MealReminderLeadTimeMinutes,
                command.WaterReminderEnabled,
                command.WaterReminderIntervalMinutes,
                command.WeeklySummaryEnabled,
                command.GoalMilestoneAlertsEnabled);

            _repository.Update(preferences);
        }

        await _unitOfWork.CommitAsync(ct);
    }
}
