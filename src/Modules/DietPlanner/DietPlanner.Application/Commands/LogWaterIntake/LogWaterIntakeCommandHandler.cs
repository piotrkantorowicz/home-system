namespace DietPlanner.Application.Commands.LogWaterIntake;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class LogWaterIntakeCommandHandler(
    IWaterIntakeRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<LogWaterIntakeCommand, Guid>
{
    public async Task<Guid> HandleAsync(LogWaterIntakeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var id = WaterIntakeId.New();
        var intake = WaterIntake.Create(id, command.UserId, command.Date, command.AmountMl, command.Note, now);

        await repository.AddAsync(intake, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
