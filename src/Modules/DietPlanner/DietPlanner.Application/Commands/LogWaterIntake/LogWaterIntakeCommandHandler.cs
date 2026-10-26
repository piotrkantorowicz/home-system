namespace DietPlanner.Application.Commands.LogWaterIntake;

using DietPlanner.Application.OperationReceipts;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class LogWaterIntakeCommandHandler(
    IWaterIntakeRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    OperationReplay replay) : ICommandHandler<LogWaterIntakeCommand, Guid>
{
    private const string Operation = "log_water_intake";

    public async Task<Guid> HandleAsync(LogWaterIntakeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var id = WaterIntakeId.New();
        // The command carries no auth subject; the person id scopes the receipt to the caller the same way.
        if (command.OperationKey is { } key
            && await replay.ClaimOrReplayAsync(command.PersonId.ToString(), Operation, key,
                new { command.Date, command.AmountMl, command.Note }, id.Value, now, ct) is { } original)
            return original;

        var intake = WaterIntake.Create(id, command.PersonId, command.Date, command.AmountMl, command.Note, now);

        await repository.AddAsync(intake, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
