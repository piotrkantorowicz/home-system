namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class BulkCompleteMealEntriesCommandHandler
    : ICommandHandler<BulkCompleteMealEntriesCommand, BulkCompleteResult>
{
    private readonly IMealEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkCompleteMealEntriesCommandHandler(
        IMealEntryRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<BulkCompleteResult> HandleAsync(
        BulkCompleteMealEntriesCommand command, CancellationToken ct = default)
    {
        var entries = await _repository.GetByUserAndDateRangeAsync(
            command.UserId, command.Date, command.Date, ct);

        var completed = 0;
        foreach (var entry in entries.Where(e => e.Status == MealEntryStatus.Planned))
        {
            entry.MarkDone();
            _repository.Update(entry);
            completed++;
        }

        if (completed > 0)
            await _unitOfWork.CommitAsync(ct);

        return new BulkCompleteResult(completed);
    }
}
