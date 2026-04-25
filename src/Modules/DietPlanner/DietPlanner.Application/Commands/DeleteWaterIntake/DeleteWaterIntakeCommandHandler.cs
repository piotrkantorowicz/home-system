namespace DietPlanner.Application.Commands.DeleteWaterIntake;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

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

        if (intake.UserId != command.UserId)
            throw new DietPlannerDomainException("You can only delete your own water intake entries.");

        _repository.Delete(intake);
        await _unitOfWork.CommitAsync(ct);
    }
}
