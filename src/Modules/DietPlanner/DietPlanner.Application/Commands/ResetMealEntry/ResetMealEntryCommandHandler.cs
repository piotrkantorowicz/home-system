namespace DietPlanner.Application.Commands.ResetMealEntry;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ResetMealEntryCommandHandler : ICommandHandler<ResetMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResetMealEntryCommandHandler(IMealEntryRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(ResetMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        if (entry.PersonId != command.PersonId)
            throw new NotFoundException("MealEntry", command.Id);

        entry.Reset();
        _repository.Update(entry);
        await _unitOfWork.CommitAsync(ct);
    }
}
