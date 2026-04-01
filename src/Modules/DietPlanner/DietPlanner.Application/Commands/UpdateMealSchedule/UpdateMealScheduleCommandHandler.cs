namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class UpdateMealScheduleCommandHandler : ICommandHandler<UpdateMealScheduleCommand>
{
    private readonly IMealScheduleConfigRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMealScheduleCommandHandler(IMealScheduleConfigRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateMealScheduleCommand command, CancellationToken ct = default)
    {
        var slots = command.Slots
            .Select(s => (s.Name, TimeOnly.Parse(s.DefaultTime)))
            .ToList();

        MealScheduleConfig? config = await _repository.GetByUserIdAsync(command.UserId, ct);

        if (config is null)
        {
            config = MealScheduleConfig.Create(MealScheduleConfigId.New(), command.UserId, slots);
            await _repository.AddAsync(config, ct);
        }
        else
        {
            config.UpdateSlots(slots);
            _repository.Update(config);
        }

        await _unitOfWork.CommitAsync(ct);
    }
}
