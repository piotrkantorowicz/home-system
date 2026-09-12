namespace DietPlanner.Application.Commands.LogWaterIntake;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class LogWaterIntakeCommandHandler : ICommandHandler<LogWaterIntakeCommand, Guid>
{
    private readonly IWaterIntakeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public LogWaterIntakeCommandHandler(IWaterIntakeRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<Guid> HandleAsync(LogWaterIntakeCommand command, CancellationToken ct = default)
    {
        var id = WaterIntakeId.New();
        var intake = WaterIntake.Create(id, command.UserId, command.Date, command.AmountMl, command.Note);

        await _repository.AddAsync(intake, ct);
        await _unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
