namespace DietPlanner.Application.Commands.CreateMealEntry;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class CreateMealEntryCommandHandler : ICommandHandler<CreateMealEntryCommand, Guid>
{
    private readonly IMealEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMealEntryCommandHandler(IMealEntryRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<Guid> HandleAsync(CreateMealEntryCommand command, CancellationToken ct = default)
    {
        var id = MealEntryId.New();
        var entry = MealEntry.Create(id, command.UserId, command.Date, command.MealType,
            RecipeId.From(command.RecipeId), command.Servings, command.Notes,
            command.MealTime, command.SequenceOrder);

        await _repository.AddAsync(entry, ct);
        await _unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
