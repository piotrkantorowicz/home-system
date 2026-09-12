namespace DietPlanner.Application.Commands.UpdateMealEntry;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateMealEntryCommandHandler : ICommandHandler<UpdateMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IMealScheduleConfigRepository _scheduleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMealEntryCommandHandler(
        IMealEntryRepository repository,
        IMealScheduleConfigRepository scheduleRepository,
        IUnitOfWork unitOfWork)
        => (_repository, _scheduleRepository, _unitOfWork)
            = (repository, scheduleRepository, unitOfWork);

    public async Task HandleAsync(UpdateMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        if (entry.UserId != command.UserId)
            throw new DietPlannerDomainException("You can only update your own meal entries.");

        var slotId = MealSlotId.From(command.MealSlotId);
        var schedule = await _scheduleRepository.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("MealScheduleConfig", command.UserId);

        if (schedule.Slots.All(s => s.Id != slotId))
            throw new NotFoundException("MealSlot", command.MealSlotId);

        entry.Update(command.Date, slotId, RecipeId.From(command.RecipeId),
            command.Servings, command.Notes, command.MealTime, command.SequenceOrder);

        _repository.Update(entry);
        await _unitOfWork.CommitAsync(ct);
    }
}
