namespace DietPlanner.Application.Commands.CreateGoal;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateGoalCommandHandler(
    IUserGoalRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateGoalCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateGoalCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var id = UserGoalId.New();
        var goal = UserGoal.Create(id, command.PersonId, now, command.DailyCalorieTarget,
            command.ProteinGrams, command.CarbsGrams, command.FatGrams, command.FiberGrams);

        await repository.AddAsync(goal, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
