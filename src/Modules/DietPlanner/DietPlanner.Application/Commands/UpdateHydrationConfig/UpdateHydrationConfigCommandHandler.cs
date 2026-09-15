namespace DietPlanner.Application.Commands.UpdateHydrationConfig;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateHydrationConfigCommandHandler(
    IHydrationConfigRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateHydrationConfigCommand>
{
    public async Task HandleAsync(UpdateHydrationConfigCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var existing = await repository.GetByUserIdAsync(command.UserId, ct);

        if (existing is not null)
        {
            existing.Update(command.DailyWaterTargetMl, command.GlassSizeMl, command.TrackWaterIntake, now);
            repository.Update(existing);
        }
        else
        {
            var config = HydrationConfig.Create(
                HydrationConfigId.New(),
                command.UserId,
                now,
                command.DailyWaterTargetMl,
                command.GlassSizeMl,
                command.TrackWaterIntake);

            await repository.AddAsync(config, ct);
        }

        await unitOfWork.CommitAsync(ct);
    }
}
