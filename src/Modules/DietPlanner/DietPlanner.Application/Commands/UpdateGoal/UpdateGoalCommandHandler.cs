namespace DietPlanner.Application.Commands.UpdateGoal;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class UpdateGoalCommandHandler : ICommandHandler<UpdateGoalCommand>
{
    private readonly IUserGoalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalCommandHandler(IUserGoalRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateGoalCommand command, CancellationToken ct = default)
    {
        var goal = await _repository.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserGoal", command.UserId);

        if (goal.UserId != command.UserId)
            throw new DietPlannerDomainException("You can only update your own goals.");

        goal.Update(command.DailyCalorieTarget, command.ProteinGrams, command.CarbsGrams,
            command.FatGrams, command.FiberGrams);

        _repository.Update(goal);
        await _unitOfWork.CommitAsync(ct);
    }
}
