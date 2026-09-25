namespace DietPlanner.Application.Commands.DeleteMealEntry;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteMealEntryCommandHandler : ICommandHandler<DeleteMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMealEntryCommandHandler(IMealEntryRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(DeleteMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        if (entry.PersonId != command.PersonId)
            throw new DietPlannerDomainException("You can only delete your own meal entries.");

        _repository.Delete(entry);
        await _unitOfWork.CommitAsync(ct);
    }
}
