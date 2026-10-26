namespace DietPlanner.Application.Commands.DeleteWaterIntake;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteWaterIntakeCommandHandler : ICommandHandler<DeleteWaterIntakeCommand>
{
    private readonly IWaterIntakeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWaterIntakeCommandHandler(IWaterIntakeRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(DeleteWaterIntakeCommand command, CancellationToken ct = default)
    {
        var intake = await _repository.GetByIdAsync(WaterIntakeId.From(command.Id), ct)
            ?? throw new NotFoundException("WaterIntake", command.Id);

        // Someone else's entry reads as missing, so its id does not confirm it exists.
        if (intake.PersonId != command.PersonId)
            throw new NotFoundException("WaterIntake", command.Id);

        _repository.Delete(intake);
        await _unitOfWork.CommitAsync(ct);
    }
}
