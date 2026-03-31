namespace DietPlanner.Application.Commands.UpdateMealEntry;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class UpdateMealEntryCommandHandler : ICommandHandler<UpdateMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMealEntryCommandHandler(IMealEntryRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        if (entry.UserId != command.UserId)
            throw new DietPlannerDomainException("You can only update your own meal entries.");

        entry.Update(command.Date, command.MealType, RecipeId.From(command.RecipeId),
            command.Servings, command.Notes, command.MealTime, command.SequenceOrder);

        _repository.Update(entry);
        await _unitOfWork.CommitAsync(ct);
    }
}
