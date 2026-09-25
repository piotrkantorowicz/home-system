namespace DietPlanner.Application.Commands.UpdateGoal;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateGoalCommandHandler(
    IUserGoalRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateGoalCommand>
{
    public async Task HandleAsync(UpdateGoalCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var goal = await repository.GetByPersonIdAsync(command.PersonId, ct)
            ?? throw new NotFoundException("UserGoal", command.PersonId);

        if (goal.PersonId != command.PersonId)
            throw new DietPlannerDomainException("You can only update your own goals.");

        goal.Update(command.DailyCalorieTarget, command.ProteinGrams, command.CarbsGrams,
            command.FatGrams, command.FiberGrams, now);

        repository.Update(goal);
        await unitOfWork.CommitAsync(ct);
    }
}
