namespace DietPlanner.Application.Commands.CreateMealEntry;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateMealEntryCommandHandler : ICommandHandler<CreateMealEntryCommand, Guid>
{
    private readonly IMealEntryRepository _repository;
    private readonly IMealScheduleConfigRepository _scheduleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMealEntryCommandHandler(
        IMealEntryRepository repository,
        IMealScheduleConfigRepository scheduleRepository,
        IUnitOfWork unitOfWork)
        => (_repository, _scheduleRepository, _unitOfWork)
            = (repository, scheduleRepository, unitOfWork);

    public async Task<Guid> HandleAsync(CreateMealEntryCommand command, CancellationToken ct = default)
    {
        var slotId = MealSlotId.From(command.MealSlotId);
        var schedule = await _scheduleRepository.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("MealScheduleConfig", command.UserId);

        if (schedule.Slots.All(s => s.Id != slotId))
            throw new NotFoundException("MealSlot", command.MealSlotId);

        var id = MealEntryId.New();
        var entry = MealEntry.Create(id, command.UserId, command.Date, slotId,
            RecipeId.From(command.RecipeId), command.Servings, command.Notes,
            command.MealTime, command.SequenceOrder);

        await _repository.AddAsync(entry, ct);
        await _unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
