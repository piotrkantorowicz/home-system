namespace DietPlanner.Application.Commands.CompleteMealEntry;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class CompleteMealEntryCommandHandler : ICommandHandler<CompleteMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteMealEntryCommandHandler(IMealEntryRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(CompleteMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        if (entry.UserId != command.UserId)
            throw new NotFoundException("MealEntry", command.Id);

        entry.MarkDone();
        _repository.Update(entry);
        await _unitOfWork.CommitAsync(ct);
    }
}
