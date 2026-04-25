namespace DietPlanner.Application.Commands.UpdateHydrationConfig;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class UpdateHydrationConfigCommandHandler : ICommandHandler<UpdateHydrationConfigCommand>
{
    private readonly IHydrationConfigRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHydrationConfigCommandHandler(IHydrationConfigRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateHydrationConfigCommand command, CancellationToken ct = default)
    {
        var existing = await _repository.GetByUserIdAsync(command.UserId, ct);

        if (existing is not null)
        {
            existing.Update(command.DailyWaterTargetMl, command.GlassSizeMl, command.TrackWaterIntake);
            _repository.Update(existing);
        }
        else
        {
            var config = HydrationConfig.Create(
                HydrationConfigId.New(),
                command.UserId,
                command.DailyWaterTargetMl,
                command.GlassSizeMl,
                command.TrackWaterIntake);

            await _repository.AddAsync(config, ct);
        }

        await _unitOfWork.CommitAsync(ct);
    }
}
